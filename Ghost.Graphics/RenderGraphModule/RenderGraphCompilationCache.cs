using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using System.Diagnostics.CodeAnalysis;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Represents cached compilation results for a render graph.
/// This avoids recompiling the graph when the structure hasn't changed.
/// </summary>
internal sealed class CachedCompilation
{
    // Compiled pass indices (indices into the _passes list)
    public readonly List<int> compiledPassIndices = new(64);

    // Culling decisions for each pass
    public readonly List<bool> passCulledFlags = new(64);
    
    // Physical resource aliasing mappings (logical index -> physical index)
    public readonly Dictionary<int, int> logicalToPhysical = new(128);
    
    // Placed resource metadata
    public readonly List<PlacedResourceData> placedResources = new(32);
    
    // Resource barriers
    public readonly List<ResourceBarrier> barriers = new(128);
    
    // Resource state mappings (for barrier generation)
    public readonly Dictionary<int, ResourceState> resourceStates = new(128);

    // Real gpu resource
    public readonly List<Handle<GPUResource>> backingResources = new(32);
    
    // View state used for this compilation
    public ViewState viewState;

    public void Clear()
    {
        compiledPassIndices.Clear();
        passCulledFlags.Clear();
        logicalToPhysical.Clear();
        placedResources.Clear();
        barriers.Clear();
        resourceStates.Clear();
        backingResources.Clear();
        viewState = default;
    }
}

/// <summary>
/// Placed resource data for caching.
/// </summary>
internal struct PlacedResourceData
{
    public int index;
    public RenderGraphResourceType type;
    public ulong heapOffset;
    public ulong sizeInBytes;
    public int firstUsePass;
    public int lastUsePass;
}

/// <summary>
/// Manages compilation caching for render graphs.
/// Stores compiled results and allows cache hits when graph structure is unchanged.
/// </summary>
internal sealed class RenderGraphCompilationCache
{
    private readonly CachedCompilation _cached = new();
    private ulong _cachedHash;
    private bool _hasCachedData;

    // Statistics
    public int CacheHits { get; private set; }
    public int CacheMisses { get; private set; }

    /// <summary>
    /// Attempts to retrieve cached compilation results.
    /// </summary>
    public bool TryGetCached(ulong hash, [MaybeNullWhen(false)] out CachedCompilation result)
    {
        if (_hasCachedData && _cachedHash == hash)
        {
            result = _cached;
            CacheHits++;
            return true;
        }

        result = null;
        CacheMisses++;
        return false;
    }

    /// <summary>
    /// Stores compilation results in the cache.
    /// </summary>
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
        _cached.barriers.AddRange(data.barriers);
        
        foreach (var kvp in data.resourceStates)
        {
            _cached.resourceStates[kvp.Key] = kvp.Value;
        }

        _cached.backingResources.AddRange(data.backingResources);
    }

    /// <summary>
    /// Invalidates the cache, forcing recompilation on next Compile().
    /// </summary>
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

    /// <summary>
    /// Gets cache statistics for debugging.
    /// </summary>
    public (int hits, int misses, double hitRate) GetStatistics()
    {
        int total = CacheHits + CacheMisses;
        double hitRate = total > 0 ? (double)CacheHits / total : 0.0;
        return (CacheHits, CacheMisses, hitRate);
    }
}
