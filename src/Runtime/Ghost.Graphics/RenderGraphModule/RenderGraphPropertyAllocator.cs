using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Allocates shader property buffers inside a 1MB persistently mapped upload ring buffer
/// and creates raw SRV descriptors for individual property slices.
/// </summary>
internal sealed unsafe class RenderGraphPropertyAllocator : IDisposable
{
    public const uint RING_BUFFER_SIZE = 1024 * 1024; // 1 MB
    public const uint ALIGNMENT = 256; // 256 bytes

    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;

    private Handle<GPUBuffer> _ringBuffer;
    private byte* _pMappedBase;
    private uint _ringOffset;

    private UnsafeList<uint> _allocatedDescriptors;
    private bool _disposed;

    public RenderGraphPropertyAllocator(IResourceAllocator allocator, IResourceDatabase database)
    {
        _allocator = allocator;
        _database = database;
        _allocatedDescriptors = new UnsafeList<uint>(64, AllocationHandle.Persistent);

        var desc = new BufferDesc
        {
            Size = RING_BUFFER_SIZE,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload,
        };

        _ringBuffer = _allocator.CreateBuffer(in desc, "RenderGraph_PropertyRingBuffer");
        if (_ringBuffer.IsValid)
        {
            _pMappedBase = (byte*)_database.MapResource(_ringBuffer.AsResource(), 0, null);
        }
    }

    /// <summary>
    /// Resets the ring buffer offset and releases all transient SRV descriptors created for the previous frame.
    /// </summary>
    public void Reset()
    {
        for (var i = 0; i < _allocatedDescriptors.Count; i++)
        {
            _database.ReleaseRawBufferSRV(_allocatedDescriptors[i]);
        }
        _allocatedDescriptors.Clear();
        _ringOffset = 0;
    }

    /// <summary>
    /// Copies the property data into the ring buffer and returns its bindless raw buffer SRV index.
    /// </summary>
    public uint Allocate<TProperty>(scoped in TProperty property) where TProperty : unmanaged
    {
        if (_pMappedBase == null || !_ringBuffer.IsValid)
        {
            return uint.MaxValue;
        }

        var size = (uint)sizeof(TProperty);
        var alignedSize = (size + 3u) & ~3u;

        if (_ringOffset + alignedSize > RING_BUFFER_SIZE)
        {
            // Wrap around ring buffer
            _ringOffset = 0;
        }

        var currentOffset = _ringOffset;
        _ringOffset = (_ringOffset + alignedSize + (ALIGNMENT - 1u)) & ~(ALIGNMENT - 1u);

        fixed (TProperty* pProp = &property)
        {
            MemoryUtility.MemCpy(_pMappedBase + currentOffset, pProp, size);
        }

        var descriptorIndex = _database.AllocateRawBufferSRV(_ringBuffer, currentOffset, alignedSize);
        if (descriptorIndex != uint.MaxValue)
        {
            _allocatedDescriptors.Add(descriptorIndex);
        }

        return descriptorIndex;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Reset();

        if (_ringBuffer.IsValid)
        {
            if (_pMappedBase != null)
            {
                _database.UnmapResource(_ringBuffer.AsResource(), 0, null);
                _pMappedBase = null;
            }

            _database.ReleaseResource(_ringBuffer.AsResource());
            _ringBuffer = Handle<GPUBuffer>.Invalid;
        }

        _allocatedDescriptors.Dispose();
        _disposed = true;
    }
}
