using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Engine.Streaming;

/// <summary>
/// Manages the GPU residency table mapping global page IDs to physical 128KB pool slots.
/// If physicalSlot == 0xFFFFFFFF, the page is NOT resident in the GPU Page Pool.
/// </summary>
public sealed unsafe class GPUPageTable : IDisposable
{
    public const uint PAGE_NOT_RESIDENT = 0xFFFFFFFFu;

    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;

    private Handle<GPUBuffer> _tableBuffer;
    private uint _capacity;
    private uint _allocatedPages;
    private UnsafeArray<uint> _cpuTable;
    private UnsafeList<uint> _dirtyPages;
    private bool _isInShaderResource;
    private bool _disposed;

    public Handle<GPUBuffer> Buffer => _tableBuffer;
    public uint Capacity => _capacity;
    public uint AllocatedPages => _allocatedPages;
    public uint BindlessIndex => _resourceDatabase.GetBindlessIndex(_tableBuffer.AsResource());

    public GPUPageTable(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, uint initialCapacity = 65536)
    {
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;
        _capacity = Math.Max(initialCapacity, 1024u);

        _cpuTable = new UnsafeArray<uint>((int)_capacity, AllocationHandle.Persistent);
        for (var i = 0; i < (int)_capacity; i++)
        {
            _cpuTable[i] = PAGE_NOT_RESIDENT;
        }

        _dirtyPages = new UnsafeList<uint>(1024, AllocationHandle.Persistent);

        var desc = new BufferDesc
        {
            Size = (ulong)_capacity * sizeof(uint),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource | BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default,
        };

        _tableBuffer = _resourceAllocator.CreateBuffer(in desc, "GPU_Page_Table");
        Logger.DebugAssert(_tableBuffer.IsValid, "Failed to create GPUPageTable buffer.");
    }

    /// <summary>
    /// Allocates a contiguous range of global page IDs for a registered mesh asset.
    /// </summary>
    public uint AllocatePageRange(uint pageCount)
    {
        var startOffset = _allocatedPages;
        _allocatedPages += pageCount;
        Logger.DebugAssert(_allocatedPages <= _capacity, "Page table capacity exceeded.");
        return startOffset;
    }

    /// <summary>
    /// Sets residency for a specific global page.
    /// </summary>
    public void SetPageResidency(uint globalPageId, uint physicalSlot)
    {
        if (globalPageId < _capacity)
        {
            if (_cpuTable[(int)globalPageId] != physicalSlot)
            {
                _cpuTable[(int)globalPageId] = physicalSlot;
                _dirtyPages.Add(globalPageId);
            }
        }
    }

    public bool IsPageResident(uint globalPageId)
    {
        return globalPageId < _capacity && _cpuTable[(int)globalPageId] != PAGE_NOT_RESIDENT;
    }

    public uint GetPhysicalSlot(uint globalPageId)
    {
        return globalPageId < _capacity ? _cpuTable[(int)globalPageId] : PAGE_NOT_RESIDENT;
    }

    /// <summary>
    /// Flushes dirty page table entries to the GPU via staging/upload buffer or copy queue.
    /// </summary>
    public void FlushUpdates(ResourceManager resourceManager, IResourceDatabase resourceDatabase, ICommandBuffer cmd)
    {
        if (_dirtyPages.Count == 0) return;

        var desc = new BufferDesc
        {
            Size = (ulong)_capacity * sizeof(uint),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload,
        };

        var staging = resourceManager.CreateTransientBuffer(in desc, "PageTable_Staging");
        var pDst = (uint*)resourceDatabase.MapResource(staging.AsResource(), 0, null);
        fixed (uint* pSrc = _cpuTable.AsSpan())
        {
            System.Buffer.MemoryCopy(pSrc, pDst, _capacity * sizeof(uint), _capacity * sizeof(uint));
        }
        resourceDatabase.UnmapResource(staging.AsResource(), 0, null);

        if (_isInShaderResource)
        {
            cmd.Barrier(BarrierDesc.Buffer(_tableBuffer, BarrierSync.AllShading, BarrierSync.Copy, BarrierAccess.ShaderResource, BarrierAccess.CopyDest));
        }

        cmd.CopyBuffer(_tableBuffer, staging, 0, 0, (ulong)_capacity * sizeof(uint));
        cmd.Barrier(BarrierDesc.Buffer(_tableBuffer, BarrierSync.Copy, BarrierSync.AllShading, BarrierAccess.CopyDest, BarrierAccess.ShaderResource));
        _isInShaderResource = true;
        _dirtyPages.Clear();
    }

    public void FlushUpdates(RenderContext ctx)
    {
        FlushUpdates(ctx.ResourceManager, ctx.ResourceDatabase, ctx.CommandBuffer);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_cpuTable.IsCreated)
        {
            _cpuTable.Dispose();
        }

        if (_dirtyPages.IsCreated)
        {
            _dirtyPages.Dispose();
        }

        if (_tableBuffer.IsValid)
        {
            _resourceDatabase.ReleaseResource(_tableBuffer.AsResource());
            _tableBuffer = Handle<GPUBuffer>.Invalid;
        }
    }
}
