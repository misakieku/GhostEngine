using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Diagnostics.CodeAnalysis;

namespace Ghost.Graphics.RenderGraphModule;

internal sealed class CachedCompilation
{
    // Compiled pass indices (indices into the _passes list)
    public readonly List<int> compiledPassIndices = new(64);

    // Culling decisions for each pass
    public readonly List<bool> passCulledFlags = new(64);

    // Physical heap aliasing mappings (logical index -> physical index)
    public readonly Dictionary<int, int> logicalToPhysical = new(128);

    // Placed heap metadata
    public readonly List<PlacedResourceData> placedResources = new(32);

    // Compiled barriers (stores only target states, queries before state from ResourceManager)
    public readonly List<CompiledBarrier> compiledBarriers = new(128);

    // Real gpu heap
    public readonly List<Handle<GPUResource>> backingResources = new(32);

    // View state used for this compilation
    public ViewState viewState;

    public void Clear()
    {
        compiledPassIndices.Clear();
        passCulledFlags.Clear();
        logicalToPhysical.Clear();
        placedResources.Clear();
        compiledBarriers.Clear();
        backingResources.Clear();
        viewState = default;
    }
}

internal struct PlacedResourceData
{
    public int index;
    public RenderGraphResourceType type;
    public ulong heapOffset;
    public ulong sizeInBytes;
    public int firstUsePass;
    public int lastUsePass;
}

internal sealed class RenderGraphCompilationCache
{
    private readonly CachedCompilation _cached = new();
    private ulong _cachedHash;
    private bool _hasCachedData;

    public bool TryGetCached(ulong hash, [MaybeNullWhen(false)] out CachedCompilation result)
    {
        if (_hasCachedData && _cachedHash == hash)
        {
            result = _cached;
            return true;
        }

        result = null;
        return false;
    }

    public void Store(ulong hash, CachedCompilation data)
    {
        _cachedHash = hash;
        _hasCachedData = true;

        // Deep copy the data
        _cached.Clear();

        _cached.compiledPassIndices.AddRange(data.compiledPassIndices);
        _cached.passCulledFlags.AddRange(data.passCulledFlags);

        foreach (var kvp in data.logicalToPhysical)
        {
            _cached.logicalToPhysical[kvp.Key] = kvp.Value;
        }

        _cached.placedResources.AddRange(data.placedResources);
        _cached.compiledBarriers.AddRange(data.compiledBarriers);

        _cached.backingResources.AddRange(data.backingResources);
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
