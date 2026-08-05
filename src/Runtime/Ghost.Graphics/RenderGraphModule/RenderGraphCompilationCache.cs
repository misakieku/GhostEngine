using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Ghost.Graphics.RenderGraphModule;

internal struct CachedCompilation : IDisposable
{
    // Compiled pass indices (indices into the _passes list)
    public UnsafeArray<int> compiledPassIndices;

    // Culling decisions for each pass
    public UnsafeArray<bool> passCulledFlags;

    // Native render passes (merged logical passes)
    public UnsafeArray<NativeRenderPass> nativePasses;

    // Physical heap aliasing mappings (logical index -> physical index)
    public UnsafeHashMap<int, int> logicalToPhysical;

    // Placed heap metadata
    public UnsafeArray<PlacedResourceData> placedResources;
    public UnsafeArray<int> aliasedLogicalResources;
    public ulong totalHeapSize;

    // Scheduled lifetime metadata used by cache-hit native-pass restoration and diagnostics.
    public UnsafeArray<int> resourceFirstUseScheduleIndices;
    public UnsafeArray<int> resourceLastUseScheduleIndices;

    // Real gpu heap
    public UnsafeArray<Handle<GPUResource>> backingResources;

    // Compiled binary command stream
    public UnsafeArray<byte> commandBytes;

    // View state used for this compilation
    public ViewState viewState;

    public void Dispose()
    {
        for (var i = 0; i < nativePasses.Length; i++)
        {
            nativePasses[i].Dispose();
        }

        compiledPassIndices.Dispose();
        passCulledFlags.Dispose();
        nativePasses.Dispose();
        logicalToPhysical.Dispose();
        placedResources.Dispose();
        aliasedLogicalResources.Dispose();
        resourceFirstUseScheduleIndices.Dispose();
        resourceLastUseScheduleIndices.Dispose();
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
    public int aliasedLogicalResourceOffset;
    public int aliasedLogicalResourceCount;
}

internal sealed class RenderGraphCompilationCache : IDisposable
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

    public ref readonly CachedCompilation SetCached(
        RenderGraphResourceRegistry registry,
        ulong hash,
        ViewState viewState,
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        ReadOnlySpan<NativeRenderPass> nativePasses,
        ReadOnlySpan<byte> commandBytes,
        AliasingPlan aliasingPlan,
        RenderGraphResourceOrdering resourceOrdering,
        AllocationHandle allocationHandle)
    {
        _cachedHash = hash;
        _hasCachedData = true;
        _cached.Dispose();

        var aliasedLogicalResourceCount = 0;
        for (var i = 0; i < aliasingPlan.placedResources.Count; i++)
        {
            aliasedLogicalResourceCount += aliasingPlan.placedResources[i].aliasedLogicalResources.Count;
        }

        var resourceFirstUseScheduleIndices = resourceOrdering.GetFirstUseScheduleIndices();
        var resourceLastUseScheduleIndices = resourceOrdering.GetLastUseScheduleIndices();

        _cached = new CachedCompilation
        {
            compiledPassIndices = new UnsafeArray<int>(compiledPasses.Length, allocationHandle),
            passCulledFlags = new UnsafeArray<bool>(passes.Count, allocationHandle),
            nativePasses = new UnsafeArray<NativeRenderPass>(nativePasses.Length, allocationHandle),
            logicalToPhysical = new UnsafeHashMap<int, int>(128, allocationHandle),
            placedResources = new UnsafeArray<PlacedResourceData>(aliasingPlan.placedResources.Count, allocationHandle),
            aliasedLogicalResources = new UnsafeArray<int>(aliasedLogicalResourceCount, allocationHandle),
            totalHeapSize = aliasingPlan.totalHeapSize,
            resourceFirstUseScheduleIndices = new UnsafeArray<int>(resourceFirstUseScheduleIndices.Length, allocationHandle),
            resourceLastUseScheduleIndices = new UnsafeArray<int>(resourceLastUseScheduleIndices.Length, allocationHandle),
            backingResources = new UnsafeArray<Handle<GPUResource>>(registry.ResourceCount, allocationHandle),
            commandBytes = new UnsafeArray<byte>(commandBytes.Length, allocationHandle),
            viewState = viewState
        };

        _cached.compiledPassIndices.CopyFrom(compiledPasses);
        _cached.nativePasses.CopyFrom(nativePasses);
        _cached.commandBytes.CopyFrom(commandBytes);
        if (!resourceFirstUseScheduleIndices.IsEmpty)
        {
            _cached.resourceFirstUseScheduleIndices.CopyFrom(resourceFirstUseScheduleIndices);
            _cached.resourceLastUseScheduleIndices.CopyFrom(resourceLastUseScheduleIndices);
        }

        for (var i = 0; i < nativePasses.Length; i++)
        {
            ref var cachedNativePass = ref _cached.nativePasses[i];
            ref readonly var srcNativePass = ref nativePasses[i];

            cachedNativePass.mergedPassIndices = new UnsafeList<int>(srcNativePass.mergedPassIndices.Count, allocationHandle);
            cachedNativePass.mergedPassIndices.CopyFrom(srcNativePass.mergedPassIndices);
            cachedNativePass.mergedPassIndices.UnsafeSetCount(srcNativePass.mergedPassIndices.Count);
        }

        for (var i = 0; i < passes.Count; i++)
        {
            _cached.passCulledFlags[i] = passes[i].culled;
        }

        aliasingPlan.StoreToCache(ref _cached.logicalToPhysical, ref _cached.placedResources, ref _cached.aliasedLogicalResources);

        for (var i = 0; i < registry.ResourceCount; i++)
        {
            var res = registry.Resources[i];
            _cached.backingResources[i] = res.backingResource;
        }

        return ref _cached;
    }

    public void Invalidate()
    {
        _hasCachedData = false;
        _cachedHash = 0;
        _cached.Dispose();
    }

    public void UpdateBackingResource(int logicalIndex, Handle<GPUResource> resource)
    {
        if (logicalIndex < 0 || logicalIndex >= _cached.backingResources.Count || !_cached.backingResources.IsCreated)
        {
            return;
        }

        _cached.backingResources[logicalIndex] = resource;
    }

    public void Dispose()
    {
        _cached.Dispose();
    }
}
