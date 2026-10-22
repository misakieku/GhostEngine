using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Engine.Streaming;

/// <summary>
/// Manages the centralized GPU Page Pool buffer (divided into 128KB physical slots).
/// </summary>
public sealed unsafe class GPUPagePool : IDisposable
{
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;

    private Handle<GPUBuffer> _poolBuffer;
    private readonly uint _slotCount;
    private readonly ulong _totalSizeBytes;
    private uint _allocatedCount;
    private UnsafeList<uint> _freeSlots;
    private bool _disposed;

    public Handle<GPUBuffer> Buffer => _poolBuffer;
    public uint SlotCount => _slotCount;
    public uint AllocatedCount => _allocatedCount;
    public ulong TotalSizeBytes => _totalSizeBytes;
    public uint BindlessIndex => _resourceDatabase.GetBindlessIndex(_poolBuffer.AsResource());

    public GPUPagePool(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, uint slotCount)
    {
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;
        _slotCount = Math.Max(slotCount, 1u);
        _totalSizeBytes = (ulong)_slotCount * (ulong)MeshletPageConstants.PAGE_SIZE;

        var desc = new BufferDesc
        {
            Size = _totalSizeBytes,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource | BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default,
        };

        _poolBuffer = _resourceAllocator.CreateBuffer(in desc, "GPU_Page_Pool");
        Logger.DebugAssert(_poolBuffer.IsValid, "Failed to create GPUPagePool buffer.");

        _freeSlots = new UnsafeList<uint>((int)_slotCount, AllocationHandle.Persistent);
        for (var i = (int)_slotCount - 1; i >= 0; i--)
        {
            _freeSlots.Add((uint)i);
        }
    }

    public bool TryAllocateSlot(out uint physicalSlot)
    {
        if (_freeSlots.Count > 0)
        {
            physicalSlot = _freeSlots[_freeSlots.Count - 1];
            _freeSlots.RemoveAt(_freeSlots.Count - 1);
            _allocatedCount++;
            return true;
        }

        physicalSlot = uint.MaxValue;
        return false;
    }

    public void FreeSlot(uint physicalSlot)
    {
        if (physicalSlot < _slotCount)
        {
            _freeSlots.Add(physicalSlot);
            if (_allocatedCount > 0)
            {
                _allocatedCount--;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_freeSlots.IsCreated)
        {
            _freeSlots.Dispose();
        }

        if (_poolBuffer.IsValid)
        {
            _resourceDatabase.ReleaseResource(_poolBuffer.AsResource());
            _poolBuffer = Handle<GPUBuffer>.Invalid;
        }
    }
}
