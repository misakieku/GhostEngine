using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.RenderGraphModule;

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
    }

    /// <summary>
    /// Compiles the render graph by culling passes, allocating resources, and preparing barriers.
    /// </summary>
    public Result<float2, Error> Compile(
        in ViewState viewState,
        ulong graphHash,
        List<RenderGraphPassBase> passes,
        List<RenderGraphPassBase> compiledPasses,
        List<NativeRenderPass> nativePasses,
        List<CompiledBarrier> compiledBarriers,
        AliasingPlan aliasingPlan,
        RenderGraphObjectPool pool)
    {
        Error error;

        // Try to restore from cache
        if (_compilationCache.TryGetCached(graphHash, out var cached))
        {
            // Check if view state changed
            var scale = viewState.CalculateScale(cached.viewState);
            if (math.any(scale > float2.one))
            {
                // View state changed - re-resolve sizes and recreate GPU resources
                _resources.ResolveTextureSizes(in viewState);
                RestoreFromCache(cached, compiledPasses, passes, nativePasses, compiledBarriers, aliasingPlan, pool);

                RenderGraphAliasingBuilder.Build(aliasingPlan, _resources, _resourceAllocator, pool);
                error = _resources.AllocateBackingResources(aliasingPlan, _compilationCache);
                if (error != Error.None)
                {
                    return error;
                }

                cached.viewState = viewState;

                return float2.one;
            }
            else
            {
                // Perfect cache hit - restore everything
                RestoreFromCache(cached, compiledPasses, passes, nativePasses, compiledBarriers, aliasingPlan, pool);
                _resources.RestoreBackingResources(cached.backingResources);
                return scale;
            }
        }

        // Fresh compilation needed
        compiledPasses.Clear();

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
                compiledPasses.Add(pass);
            }
        }

        RenderGraphAliasingBuilder.Build(aliasingPlan, _resources, _resourceAllocator, pool);
        error = _resources.AllocateBackingResources(aliasingPlan, _compilationCache);
        if (error != Error.None)
        {
            return error;
        }

        RenderGraphBarriers.CompileBarriers(compiledPasses, compiledBarriers, _resources, aliasingPlan);
        _nativePassBuilder.BuildNativeRenderPasses(compiledPasses, nativePasses, compiledBarriers);
        StoreInCache(graphHash, viewState, compiledPasses, passes, compiledBarriers, aliasingPlan);

        return float2.one;
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

    private void RestoreFromCache(
        CachedCompilation cached,
        List<RenderGraphPassBase> compiledPasses,
        List<RenderGraphPassBase> passes,
        List<NativeRenderPass> nativePasses,
        List<CompiledBarrier> compiledBarriers,
        AliasingPlan aliasingPlan,
        RenderGraphObjectPool pool)
    {
        compiledPasses.Clear();
        for (var i = 0; i < cached.compiledPassIndices.Count; i++)
        {
            var passIndex = cached.compiledPassIndices[i];
            compiledPasses.Add(passes[passIndex]);
        }

        for (var i = 0; i < passes.Count && i < cached.passCulledFlags.Count; i++)
        {
            passes[i].culled = cached.passCulledFlags[i];
        }

        RenderGraphAliasingBuilder.RestoreFromCache(aliasingPlan, cached.logicalToPhysical, cached.placedResources, pool);

        compiledBarriers.Clear();
        for (var i = 0; i < cached.compiledBarriers.Count; i++)
        {
            compiledBarriers.Add(cached.compiledBarriers[i]);
        }

        _nativePassBuilder.BuildNativeRenderPasses(compiledPasses, nativePasses, compiledBarriers);
    }

    private void StoreInCache(
        ulong graphHash,
        in ViewState viewState,
        List<RenderGraphPassBase> compiledPasses,
        List<RenderGraphPassBase> passes,
        List<CompiledBarrier> compiledBarriers,
        AliasingPlan aliasingPlan)
    {
        var cacheData = new CachedCompilation();

        cacheData.viewState = viewState;

        for (var i = 0; i < compiledPasses.Count; i++)
        {
            cacheData.compiledPassIndices.Add(compiledPasses[i].index);
        }

        for (var i = 0; i < passes.Count; i++)
        {
            cacheData.passCulledFlags.Add(passes[i].culled);
        }

        aliasingPlan.StoreToCache(cacheData.logicalToPhysical, cacheData.placedResources);

        for (var i = 0; i < compiledBarriers.Count; i++)
        {
            cacheData.compiledBarriers.Add(compiledBarriers[i]);
        }

        for (var i = 0; i < _resources.ResourceCount; i++)
        {
            var res = _resources.Resources[i];
            cacheData.backingResources.Add(res.backingResource);
        }

        _compilationCache.Store(graphHash, cacheData);
    }
}
