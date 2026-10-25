using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Allocates shader property buffers inside a 1MB persistently mapped upload ring buffer
/// at 256-byte aligned offsets for root CBV binding.
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
    private bool _disposed;

    public Handle<GPUBuffer> Buffer => _ringBuffer;

    public RenderGraphPropertyAllocator(IResourceAllocator allocator, IResourceDatabase database)
    {
        _allocator = allocator;
        _database = database;

        var desc = new BufferDesc
        {
            Size = RING_BUFFER_SIZE,
            Stride = 256,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        _ringBuffer = _allocator.CreateBuffer(in desc, "RenderGraph_PropertyRingBuffer");
        if (_ringBuffer.IsValid)
        {
            _pMappedBase = (byte*)_database.MapResource(_ringBuffer.AsResource(), 0, null);
        }
    }

    /// <summary>
    /// Resets the ring buffer offset for the new frame.
    /// </summary>
    public void Reset()
    {
        _ringOffset = 0;
    }

    /// <summary>
    /// Copies the property data into the ring buffer and returns its 256-byte aligned byte offset.
    /// </summary>
    public uint Allocate<TProperty>(scoped in TProperty property) where TProperty : unmanaged
    {
        if (_pMappedBase == null || !_ringBuffer.IsValid)
        {
            return uint.MaxValue;
        }

        var size = (uint)sizeof(TProperty);
        var alignedSize = (size + (ALIGNMENT - 1u)) & ~(ALIGNMENT - 1u);

        if (_ringOffset + alignedSize > RING_BUFFER_SIZE)
        {
            // Wrap around ring buffer
            _ringOffset = 0;
        }

        var currentOffset = _ringOffset;
        _ringOffset += alignedSize;

        fixed (TProperty* pProp = &property)
        {
            MemoryUtility.MemCpy(_pMappedBase + currentOffset, pProp, size);
        }

        return currentOffset;
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

        _disposed = true;
    }
}
