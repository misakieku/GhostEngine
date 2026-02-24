using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Handles compilation of the render graph including pass culling, resource allocation,
/// barrier compilation, and cache management.
/// </summary>
internal sealed class RenderGraphCompiler
{
    private readonly IResourceManager _resourceManager;
    private readonly RenderGraphResourceRegistry _resources;
    private readonly ResourceAliasingManager _aliasingManager;
    private readonly RenderGraphNativePassBuilder _nativePassBuilder;
    private readonly RenderGraphCompilationCache _compilationCache;

    private Handle<GPUResource> _resourceHeap;

    public RenderGraphCompiler(
        IResourceManager resourceManager,
        RenderGraphResourceRegistry resources,
        ResourceAliasingManager aliasingManager,
        RenderGraphNativePassBuilder nativePassBuilder,
        RenderGraphCompilationCache compilationCache)
    {
        _resourceManager = resourceManager;
        _resources = resources;
        _aliasingManager = aliasingManager;
        _nativePassBuilder = nativePassBuilder;
        _compilationCache = compilationCache;
        _resourceHeap = Handle<GPUResource>.Invalid;
    }

    /// <summary>
    /// Compiles the render graph by culling passes, allocating resources, and preparing barriers.
    /// </summary>
    public void Compile(
        in ViewState viewState,
        ulong graphHash,
        List<RenderGraphPassBase> passes,
        List<RenderGraphPassBase> compiledPasses,
        List<NativeRenderPass> nativePasses,
        List<CompiledBarrier> compiledBarriers)
    {
        // Try to restore from cache
        if (_compilationCache.TryGetCached(graphHash, out var cached))
        {
            // Check if view state changed
            if (!cached.viewState.Equals(viewState))
            {
                // View state changed - re-resolve sizes and recreate GPU resources
                _resources.ResolveTextureSizes(in viewState);
                RestoreFromCache(cached, compiledPasses, passes, nativePasses, compiledBarriers);

                _aliasingManager.AssignPhysicalResources(_resources, passes.Count);
                AllocateResources();

                cached.viewState = viewState;
            }
            else
            {
                // Perfect cache hit - restore everything
                RestoreFromCache(cached, compiledPasses, passes, nativePasses, compiledBarriers);
            }

            return;
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

        _aliasingManager.AssignPhysicalResources(_resources, passes.Count);
        AllocateResources();

        CompileBarriers(compiledPasses, compiledBarriers);
        _nativePassBuilder.BuildNativeRenderPasses(compiledPasses, nativePasses, compiledBarriers);
        StoreInCache(graphHash, viewState, compiledPasses, passes, compiledBarriers);
    }

    /// <summary>
    /// Marks passes that write to imported resources as having side effects.
    /// </summary>
    private void MarkPassesWithSideEffects(List<RenderGraphPassBase> passes)
    {
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];

            // Check if this pass writes to any imported resources
            for (var j = 0; j < (int)RenderGraphResourceType.Count; j++)
            {
                var writeList = pass.resourceWrites[j];
                for (var k = 0; k < writeList.Count; k++)
                {
                    var writeHandle = writeList[k];
                    var resource = _resources.GetResource(writeHandle);
                    if (resource.isImported)
                    {
                        pass.hasSideEffects = true;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Culls unused passes based on dependency analysis.
    /// </summary>
    private void CullPasses(List<RenderGraphPassBase> passes)
    {
        // Mark all passes as culled initially
        for (var i = 0; i < passes.Count; i++)
        {
            passes[i].culled = passes[i].allowCulling && !passes[i].hasSideEffects;
        }

        // Traverse backwards from passes with side effects
        for (var i = passes.Count - 1; i >= 0; i--)
        {
            var pass = passes[i];
            if (!pass.culled)
            {
                UnculDependencies(pass, passes);
            }
        }
    }

    /// <summary>
    /// Recursively un-culls dependencies of a pass.
    /// </summary>
    private void UnculDependencies(RenderGraphPassBase pass, List<RenderGraphPassBase> passes)
    {
        // Un-cull producers of read resources
        for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
        {
            var readList = pass.resourceReads[i];
            for (var j = 0; j < readList.Count; j++)
            {
                UnculProducer(readList[j], passes);
            }
        }

        // Un-cull producers of color attachments
        for (var i = 0; i < pass.maxColorIndex; i++)
        {
            if (pass.colorAccess[i].id.IsValid)
            {
                UnculProducer(pass.colorAccess[i].id.AsResource(), passes);
            }
        }

        // Un-cull producer of depth attachment
        if (pass.depthAccess.id.IsValid)
        {
            UnculProducer(pass.depthAccess.id.AsResource(), passes);
        }

        // Un-cull producers of UAV resources (if not already in reads/writes)
        for (var i = 0; i < pass.randomAccess.Count; i++)
        {
            UnculProducer(pass.randomAccess[i], passes);
        }
    }

    /// <summary>
    /// Un-culls the producer of a resource.
    /// </summary>
    private void UnculProducer(Identifier<RGResource> resource, List<RenderGraphPassBase> passes)
    {
        var res = _resources.GetResource(resource);
        if (res.producerPass >= 0)
        {
            var producer = passes[res.producerPass];
            if (producer.culled)
            {
                producer.culled = false;
                UnculDependencies(producer, passes);
            }
        }
    }

    /// <summary>
    /// Allocates GPU resources for the render graph.
    /// </summary>
    private void AllocateResources()
    {
        if (_resourceHeap.IsValid)
        {
            foreach (var res in _resources.Resources)
            {
                if (res.isImported)
                {
                    continue;
                }

                _resourceManager.ResourceDatabase.ScheduleReleaseResource(res.backingResource);
            }

            _resourceManager.ResourceDatabase.ScheduleReleaseResource(_resourceHeap);
        }

        if (_aliasingManager.Heap.size == 0)
        {
            return;
        }

        var allocationDesc = new AllocationDesc
        {
            Size = _aliasingManager.Heap.size + 64 * 1024, // Add 64KB padding to avoid potential overflows
            Alignment = ResourceHeap.DEFAULT_ALIGNMENT,
            HeapFlags = HeapFlags.AlowBufferAndTexture,
            HeapType = HeapType.Default
        };

        _resourceHeap = _resourceManager.ResourceAllocator.Allocate(in allocationDesc, "RenderGraphResourceHeap");

        for (var i = 0; i < _resources.Resources.Count; i++)
        {
            var placedIndex = _aliasingManager.GetPlacedResourceIndex(i);
            var placed = _aliasingManager.GetPlacedResource(placedIndex);
            if (placed == null)
            {
                continue;
            }

            var res = _resources.Resources[i];
            var ops = new CreationOptions
            {
                AllocationType = ResourceAllocationType.Suballocation,
                Heap = _resourceHeap,
                Offset = placed.heapOffset,
            };

            if (res.type == RenderGraphResourceType.Texture)
            {
                var textureDesc = res.rgTextureDesc.ToTextureDesc(res.resolvedWidth, res.resolvedHeight);
                res.backingResource = _resourceManager.ResourceAllocator.CreateTexture(in textureDesc, res.name, ops).AsResource();
            }
            else if (res.type == RenderGraphResourceType.Buffer)
            {
                res.backingResource = _resourceManager.ResourceAllocator.CreateBuffer(in res.bufferDesc, res.name, ops).AsResource();
            }
            else
            {
                throw new NotSupportedException();
            }

            _compilationCache.UpdateBackingResource(i, res.backingResource);
        }
    }

    /// <summary>
    /// Compiles all barriers needed for execution.
    /// Delegates to RenderGraphBarriers for the actual compilation logic.
    /// </summary>
    private void CompileBarriers(List<RenderGraphPassBase> compiledPasses, List<CompiledBarrier> compiledBarriers)
    {
        RenderGraphBarriers.CompileBarriers(compiledPasses, compiledBarriers, _resources, _aliasingManager);
    }

    /// <summary>
    /// Restores the render graph state from cached compilation results.
    /// </summary>
    private void RestoreFromCache(
        CachedCompilation cached,
        List<RenderGraphPassBase> compiledPasses,
        List<RenderGraphPassBase> passes,
        List<NativeRenderPass> nativePasses,
        List<CompiledBarrier> compiledBarriers)
    {
        // Restore compiled pass list
        compiledPasses.Clear();
        for (var i = 0; i < cached.compiledPassIndices.Count; i++)
        {
            var passIndex = cached.compiledPassIndices[i];
            compiledPasses.Add(passes[passIndex]);
        }

        // Restore culling flags
        for (var i = 0; i < passes.Count && i < cached.passCulledFlags.Count; i++)
        {
            passes[i].culled = cached.passCulledFlags[i];
        }

        // Restore aliasing mappings (need to update ResourceAliasingManager)
        _aliasingManager.RestoreFromCache(cached.logicalToPhysical, cached.placedResources);

        // Restore compiled barriers (deep copy to avoid shared references)
        compiledBarriers.Clear();
        for (var i = 0; i < cached.compiledBarriers.Count; i++)
        {
            compiledBarriers.Add(cached.compiledBarriers[i]);
        }

        for (var i = 0; i < _resources.ResourceCount; i++)
        {
            var res = _resources.Resources[i];

            if (!res.isImported)
            {
                res.backingResource = cached.backingResources[i];
            }
        }

        _nativePassBuilder.BuildNativeRenderPasses(compiledPasses, nativePasses, compiledBarriers);
    }

    /// <summary>
    /// Stores current compilation results in the cache.
    /// </summary>
    private void StoreInCache(
        ulong graphHash,
        in ViewState viewState,
        List<RenderGraphPassBase> compiledPasses,
        List<RenderGraphPassBase> passes,
        List<CompiledBarrier> compiledBarriers)
    {
        var cacheData = new CachedCompilation();

        // Store view state
        cacheData.viewState = viewState;

        // Store compiled pass indices
        for (var i = 0; i < compiledPasses.Count; i++)
        {
            cacheData.compiledPassIndices.Add(compiledPasses[i].index);
        }

        // Store culling flags for all passes
        for (var i = 0; i < passes.Count; i++)
        {
            cacheData.passCulledFlags.Add(passes[i].culled);
        }

        // Store aliasing mappings
        _aliasingManager.StoreToCache(cacheData.logicalToPhysical, cacheData.placedResources);

        // Store compiled barriers
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

    /// <summary>
    /// Releases allocated GPU resources.
    /// </summary>
    public void Dispose()
    {
        if (_resourceHeap.IsValid)
        {
            foreach (var res in _resources.Resources)
            {
                if (!res.isImported)
                {
                    _resourceManager.ResourceDatabase.ScheduleReleaseResource(res.backingResource);
                }
            }

            _resourceManager.ResourceDatabase.ScheduleReleaseResource(_resourceHeap);
            _resourceHeap = Handle<GPUResource>.Invalid;
        }
    }
}
