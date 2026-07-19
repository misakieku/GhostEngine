using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.RenderGraphModule;

internal struct CompiledGraph : IDisposable
{
    public AliasingPlan plan;
    public float2 scale;
    public IReadOnlyList<RenderGraphPassBase> compiledPasses;
    public IReadOnlyList<NativeRenderPass> nativePasses;
    public IReadOnlyList<CompiledBarrier> compiledBarriers;

    public void Dispose()
    {
        plan.Dispose();
    }
}

/// <summary>
/// Handles compilation of the render graph including pass culling, heap allocation orchestration,
/// barrier compilation, and cache management.
/// </summary>
internal sealed class RenderGraphCompiler
{
    private readonly IResourceAllocator _resourceAllocator;
    private readonly RenderGraphResourceRegistry _resources;
    private readonly RenderGraphNativePassBuilder _nativePassBuilder;
    private readonly RenderGraphCompilationCache _compilationCache;

    private readonly List<RenderGraphPassBase> _compiledPasses;
    private readonly List<NativeRenderPass> _nativePasses;
    private readonly List<CompiledBarrier> _compiledBarriers;

    public RenderGraphCompiler(
        IResourceAllocator resourceAllocator,
        RenderGraphResourceRegistry resources,
        RenderGraphNativePassBuilder nativePassBuilder,
        RenderGraphCompilationCache compilationCache)
    {
        _resourceAllocator = resourceAllocator;
        _resources = resources;
        _nativePassBuilder = nativePassBuilder;
        _compilationCache = compilationCache;

        _compiledPasses = new List<RenderGraphPassBase>(64);
        _nativePasses = new List<NativeRenderPass>(32);
        _compiledBarriers = new List<CompiledBarrier>(128);
    }

    /// <summary>
    /// Compiles the render graph by culling passes, allocating resources, and preparing barriers.
    /// </summary>
    public Result<CompiledGraph, Error> Compile(
        in ViewState viewState,
        ulong graphHash,
        List<RenderGraphPassBase> passes,
        AllocationHandle allocationHandle)
    {
        Error error;
        AliasingPlan aliasingPlan;

        // Try to restore from cache
        if (_compilationCache.TryGetCached(graphHash, out var cached))
        {
            // Check if view state changed
            var scale = viewState.CalculateScale(cached.viewState);
            if (math.any(scale > float2.one))
            {
                // View state changed - re-resolve sizes and recreate GPU resources
                _resources.ResolveTextureSizes(in viewState);
                // var aliasingPlan = RestoreFromCache(cached, compiledPasses, passes, nativePasses, compiledBarriers);

                aliasingPlan = RenderGraphAliasingBuilder.Build(_resources, _resourceAllocator, allocationHandle);
                error = _resources.AllocateBackingResources(aliasingPlan, _compilationCache);
                if (error != Error.None)
                {
                    return error;
                }

                cached.viewState = viewState;

                return new CompiledGraph
                {
                    scale = float2.one,
                    plan = aliasingPlan,
                    compiledPasses = _compiledPasses,
                    nativePasses = _nativePasses,
                    compiledBarriers = _compiledBarriers
                };
            }
            else
            {
                // Perfect cache hit - restore everything
                aliasingPlan = RestoreFromCache(cached, passes, allocationHandle);
                _resources.RestoreBackingResources(cached.backingResources);
                return new CompiledGraph
                {
                    scale = scale,
                    plan = aliasingPlan,
                    compiledPasses = _compiledPasses,
                    nativePasses = _nativePasses,
                    compiledBarriers = _compiledBarriers
                };
            }
        }

        // Fresh compilation needed
        _compiledPasses.Clear();

        // Mark passes with side effects (writes to imported resources)
        MarkPassesWithSideEffects(passes);

        // Cull passes based on dependency analysis
        CullPasses(passes);

        // Build final pass list (only non-culled passes)
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];
            if (!pass.culled)
            {
                _compiledPasses.Add(pass);
            }
        }

        aliasingPlan = RenderGraphAliasingBuilder.Build(_resources, _resourceAllocator, allocationHandle);
        error = _resources.AllocateBackingResources(aliasingPlan, _compilationCache);
        if (error != Error.None)
        {
            return error;
        }

        RenderGraphBarriers.CompileBarriers(_compiledPasses, _compiledBarriers, _resources, aliasingPlan);
        _nativePassBuilder.BuildNativeRenderPasses(_compiledPasses, _nativePasses, _compiledBarriers);
        StoreInCache(graphHash, viewState, passes, aliasingPlan);

        return new CompiledGraph
        {
            scale = float2.one,
            plan = aliasingPlan,
            compiledPasses = _compiledPasses,
            nativePasses = _nativePasses,
            compiledBarriers = _compiledBarriers
        };
    }

    private void MarkPassesWithSideEffects(List<RenderGraphPassBase> passes)
    {
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];

            for (var j = 0; j < (int)RenderGraphResourceType.Count; j++)
            {
                var writeList = pass.resourceWrites[j];
                for (var k = 0; k < writeList.Count; k++)
                {
                    var writeHandle = writeList[k];
                    ref readonly var resource = ref _resources.GetResource(writeHandle);
                    if (resource.isImported)
                    {
                        pass.hasSideEffects = true;
                        break;
                    }
                }
            }
        }
    }

    private void CullPasses(List<RenderGraphPassBase> passes)
    {
        for (var i = 0; i < passes.Count; i++)
        {
            passes[i].culled = passes[i].allowCulling && !passes[i].hasSideEffects;
        }

        for (var i = passes.Count - 1; i >= 0; i--)
        {
            var pass = passes[i];
            if (!pass.culled)
            {
                UncullDependencies(pass, passes);
            }
        }
    }

    private void UncullDependencies(RenderGraphPassBase pass, List<RenderGraphPassBase> passes)
    {
        for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
        {
            var readList = pass.resourceReads[i];
            for (var j = 0; j < readList.Count; j++)
            {
                UncullProducer(readList[j], passes);
            }
        }

        for (var i = 0; i <= pass.maxColorIndex; i++)
        {
            if (pass.colorAccess[i].id.IsValid)
            {
                UncullProducer(pass.colorAccess[i].id.AsResource(), passes);
            }
        }

        if (pass.depthAccess.id.IsValid)
        {
            UncullProducer(pass.depthAccess.id.AsResource(), passes);
        }

        for (var i = 0; i < pass.randomAccess.Count; i++)
        {
            UncullProducer(pass.randomAccess[i], passes);
        }
    }

    private void UncullProducer(Identifier<RGResource> resource, List<RenderGraphPassBase> passes)
    {
        ref readonly var res = ref _resources.GetResource(resource);
        if (res.producerPass >= 0)
        {
            var producer = passes[res.producerPass];
            if (producer.culled)
            {
                producer.culled = false;
                UncullDependencies(producer, passes);
            }
        }
    }

    private AliasingPlan RestoreFromCache(
        CachedCompilation cached,
        List<RenderGraphPassBase> passes,
        AllocationHandle allocationHandle)
    {
        _compiledPasses.Clear();
        for (var i = 0; i < cached.compiledPassIndices.Count; i++)
        {
            var passIndex = cached.compiledPassIndices[i];
            _compiledPasses.Add(passes[passIndex]);
        }

        for (var i = 0; i < passes.Count && i < cached.passCulledFlags.Count; i++)
        {
            passes[i].culled = cached.passCulledFlags[i];
        }

        var plan = RenderGraphAliasingBuilder.RestoreFromCache(cached.logicalToPhysical, cached.placedResources, allocationHandle);

        _compiledBarriers.Clear();
        for (var i = 0; i < cached.compiledBarriers.Count; i++)
        {
            _compiledBarriers.Add(cached.compiledBarriers[i]);
        }

        // Why we need to build this every frame?
        _nativePassBuilder.BuildNativeRenderPasses(_compiledPasses, _nativePasses, _compiledBarriers);

        return plan;
    }

    private void StoreInCache(
        ulong graphHash,
        in ViewState viewState,
        List<RenderGraphPassBase> passes,
        AliasingPlan aliasingPlan)
    {
        var cacheData = new CachedCompilation
        {
            viewState = viewState
        };

        for (var i = 0; i < _compiledPasses.Count; i++)
        {
            cacheData.compiledPassIndices.Add(_compiledPasses[i].index);
        }

        for (var i = 0; i < passes.Count; i++)
        {
            cacheData.passCulledFlags.Add(passes[i].culled);
        }

        aliasingPlan.StoreToCache(cacheData.logicalToPhysical, cacheData.placedResources);

        for (var i = 0; i < _compiledBarriers.Count; i++)
        {
            cacheData.compiledBarriers.Add(_compiledBarriers[i]);
        }

        for (var i = 0; i < _resources.ResourceCount; i++)
        {
            var res = _resources.Resources[i];
            cacheData.backingResources.Add(res.backingResource);
        }

        _compilationCache.Store(graphHash, cacheData);
    }
}
