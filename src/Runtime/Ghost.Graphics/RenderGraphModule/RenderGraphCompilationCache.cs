using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Ghost.Graphics.RenderGraphModule;

internal struct CachedCompilation : IDisposable
{
    // Compiled pass indices (indices into the _passes list)
    public UnsafeList<int> compiledPassIndices;

    // Culling decisions for each pass
    public UnsafeList<bool> passCulledFlags;

    // Physical heap aliasing mappings (logical index -> physical index)
    public UnsafeHashMap<int, int> logicalToPhysical;

    // Placed heap metadata
    public UnsafeList<PlacedResourceData> placedResources;

    // Compiled barriers (stores only target states, queries before state from ResourceManager)
    public UnsafeList<CompiledBarrier> compiledBarriers;

    // Real gpu heap
    public UnsafeList<Handle<GPUResource>> backingResources;

    // Compiled binary command stream
    public BufferWriter commandBytes;

    // View state used for this compilation
    public ViewState viewState;

    public CachedCompilation(AllocationHandle allocationHandle)
    {
        compiledPassIndices = new UnsafeList<int>(64, allocationHandle);
        passCulledFlags = new UnsafeList<bool>(64, allocationHandle);
        logicalToPhysical = new UnsafeHashMap<int, int>(128, AllocationHandle.Persistent);
        placedResources = new UnsafeList<PlacedResourceData>(32, AllocationHandle.Persistent);
        compiledBarriers = new UnsafeList<CompiledBarrier>(128, AllocationHandle.Persistent);
        backingResources = new UnsafeList<Handle<GPUResource>>(32, AllocationHandle.Persistent);
        commandBytes = new BufferWriter(1024 * 1024, AllocationHandle.Persistent); // Start with 1MB buffer
        viewState = default;
    }

    public void Clear()
    {
        compiledPassIndices.Clear();
        passCulledFlags.Clear();
        logicalToPhysical.Clear();
        placedResources.Clear();
        compiledBarriers.Clear();
        backingResources.Clear();
        commandBytes.Reset();
        viewState = default;
    }

    public void Dispose()
    {
        compiledPassIndices.Dispose();
        passCulledFlags.Dispose();
        logicalToPhysical.Dispose();
        placedResources.Dispose();
        compiledBarriers.Dispose();
        backingResources.Dispose();
        commandBytes.Dispose();
    }
}

internal struct PlacedResourceData
{
    public int index;
    public RGResourceType type;
    public ulong heapOffset;
    public ulong sizeInBytes;
    public int firstUsePass;
    public int lastUsePass;
}

internal sealed class RenderGraphCompilationCache
{
    private CachedCompilation _cached;
    private ulong _cachedHash;
    private bool _hasCachedData;

    public bool TryGetCached(ulong hash, [MaybeNullWhen(false)] out CachedCompilation result)
    {
        if (_hasCachedData && _cachedHash == hash)
        {
            result = _cached;
            return true;
        }

        result = default;
        return false;
    }

    public void Store(ulong hash, [Owner] ref CachedCompilation data)
    {
        _cachedHash = hash;
        _hasCachedData = true;

        // Deep copy the data
        _cached.Dispose();
        _cached = data;
    }

    public void Invalidate()
    {
        _hasCachedData = false;
        _cachedHash = 0;
        _cached.Clear();
    }

    public void UpdateBackingResource(int logicalIndex, Handle<GPUResource> resource)
    {
        if (logicalIndex < 0 || logicalIndex >= _cached.backingResources.Count)
        {
            return;
        }

        _cached.backingResources[logicalIndex] = resource;
    }
}
