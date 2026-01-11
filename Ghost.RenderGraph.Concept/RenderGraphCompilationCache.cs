using System.Diagnostics.CodeAnalysis;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Represents cached compilation results for a render graph.
/// This avoids recompiling the graph when the structure hasn't changed.
/// </summary>
internal sealed class CachedCompilation
{
    // Compiled pass indices (indices into the _passes list)
    public readonly List<int> CompiledPassIndices = new(64);
    
    // Culling decisions for each pass
    public readonly List<bool> PassCulledFlags = new(64);
    
    // Physical resource aliasing mappings (logical index -> physical index)
    public readonly Dictionary<int, int> LogicalToPhysical = new(128);
    
    // Physical resource metadata
    public readonly List<PhysicalResourceData> PhysicalResources = new(32);
    
    // Resource barriers
    public readonly List<ResourceBarrier> Barriers = new(128);
    
    // Resource state mappings (for barrier generation)
    public readonly Dictionary<int, ResourceState> ResourceStates = new(128);

    public void Clear()
    {
        CompiledPassIndices.Clear();
        PassCulledFlags.Clear();
        LogicalToPhysical.Clear();
        PhysicalResources.Clear();
        Barriers.Clear();
        ResourceStates.Clear();
    }
}

/// <summary>
/// Physical resource data for caching.
/// </summary>
internal struct PhysicalResourceData
{
    public int Index;
    public int Width;
    public int Height;
    public TextureFormat Format;
    public int FirstUsePass;
    public int LastUsePass;
}

/// <summary>
/// Manages compilation caching for render graphs.
/// Stores compiled results and allows cache hits when graph structure is unchanged.
/// </summary>
internal sealed class RenderGraphCompilationCache
{
    private ulong _cachedHash;
    private readonly CachedCompilation _cached = new();
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
        
        _cached.CompiledPassIndices.AddRange(data.CompiledPassIndices);
        _cached.PassCulledFlags.AddRange(data.PassCulledFlags);
        
        foreach (var kvp in data.LogicalToPhysical)
        {
            _cached.LogicalToPhysical[kvp.Key] = kvp.Value;
        }
        
        _cached.PhysicalResources.AddRange(data.PhysicalResources);
        _cached.Barriers.AddRange(data.Barriers);
        
        foreach (var kvp in data.ResourceStates)
        {
            _cached.ResourceStates[kvp.Key] = kvp.Value;
        }
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
