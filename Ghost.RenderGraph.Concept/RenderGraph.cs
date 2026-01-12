using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.IO.Hashing;
using TerraFX.Interop.Windows;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Main render graph class that manages resource allocation and pass execution.
/// 
/// Design principles for minimal GC:
/// - Object pooling for all passes and resources
/// - Reuse collections across frames (Clear() instead of new)
/// - Avoid LINQ and foreach over interfaces
/// - Pre-allocate capacity based on expected usage
/// </summary>
public sealed class RenderGraph
{
    private readonly RenderGraphResourceRegistry _resources = new();
    private readonly RenderGraphObjectPool _objectPool = new();
    private readonly List<RenderGraphPassBase> _passes = new(64);
    private readonly List<RenderGraphPassBase> _compiledPasses = new(64);
    private readonly RenderGraphBuilder _builder = new();
    private readonly MockCommandBuffer _commandBuffer = new();
    private readonly RenderContext _renderContext;
    private readonly ResourceAliasingManager _aliasingManager = new();
    private readonly Dictionary<int, ResourceState> _resourceStates = new(128);
    private readonly List<ResourceBarrier> _barriers = new(128);
    private readonly RenderGraphCompilationCache _compilationCache = new();

    private bool _compiled;

    public RenderGraphBlackboard Blackboard { get; } = new();

    public RenderGraph()
    {
        _renderContext = new RenderContext(_commandBuffer);
    }

    /// <summary>
    /// Resets the render graph for a new frame.
    /// Reuses existing allocations to minimize GC.
    /// </summary>
    public void Reset()
    {
        // Clear blackboard data
        Blackboard.Clear();

        // Reset resources but keep allocations
        _resources.BeginFrame();

        // Reset aliasing manager
        _aliasingManager.BeginFrame();

        // Clear resource states and barriers
        _resourceStates.Clear();
        _barriers.Clear();

        // Return passes to the pool and reset count
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            pass.Reset(_objectPool);
        }

        _passes.Clear();

        // Clear compiled passes list
        _compiledPasses.Clear();
        _compiled = false;
    }

    /// <summary>
    /// Imports an external texture into the render graph.
    /// </summary>
    public Identifier<RGTexture> ImportTexture(TextureDescriptor descriptor)
    {
        return _resources.ImportTexture(descriptor);
    }

    public IRasterRenderGraphBuilder AddRasterRenderPass<TPassData>(string name, out TPassData passData)
        where TPassData : class, new()
    {
        var renderPass = _objectPool.Rent<RasterRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, _objectPool.Rent<TPassData>(), name, RenderPassType.Raster);
        passData = renderPass.passData;

        _passes.Add(renderPass);

        _builder.Init(this, renderPass, _resources);
        return _builder;
    }

    public IComputeRenderGraphBuilder AddComputeRenderPass<TPassData>(string name, out TPassData passData)
        where TPassData : class, new()
    {
        var renderPass = _objectPool.Rent<ComputeRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, _objectPool.Rent<TPassData>(), name, RenderPassType.Compute);
        passData = renderPass.passData;

        _passes.Add(renderPass);

        _builder.Init(this, renderPass, _resources);
        return _builder;
    }

    /// <summary>
    /// Computes a _hasher of the render graph structure for caching.
    /// Does NOT include pass names (they don't affect compilation).
    /// Uses XxHash3 with SIMD optimizations for fast hashing.
    /// </summary>
    private unsafe ulong ComputeGraphHash()
    {
        using var scope = AllocationManager.CreateStackScope();
        var bufferPool = new UnsafeList<byte>(2048, scope.AllocationHandle);
        var pData = (byte*)bufferPool.GetUnsafePtr();
        var offset = 0;

        // Hash pass count
        *(int*)(pData + offset) = _passes.Count;
        offset += sizeof(int);

        // Hash each pass structure (excluding names)
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];

            *(RenderPassType*)(pData + offset) = pass.type;
            offset += sizeof(RenderPassType);

            *(bool*)(pData + offset) = pass.allowCulling;
            offset += sizeof(bool);

            *(bool*)(pData + offset) = pass.asyncCompute;
            offset += sizeof(bool);

            *(TextureAccess*)(pData + offset) = pass.depthAccess;
            offset += sizeof(TextureAccess);

            *(int*)(pData + offset) = pass.maxColorIndex;
            offset += sizeof(int);
            for (var j = 0; j <= pass.maxColorIndex; j++)
            {
                *(TextureAccess*)(pData + offset) = pass.colorAccess[j];
                offset += sizeof(TextureAccess);
            }

            *(int*)(pData + offset) = pass.resourceReads.Count;
            offset += sizeof(int);
            for (var j = 0; j < pass.resourceReads.Count; j++)
            {
                *(int*)(pData + offset) = pass.resourceReads[j].Value;
                offset += sizeof(int);
            }

            *(int*)(pData + offset) = pass.resourceWrites.Count;
            offset += sizeof(int);
            for (var j = 0; j < pass.resourceWrites.Count; j++)
            {
                *(int*)(pData + offset) = pass.resourceWrites[j].Value;
                offset += sizeof(int);
            }

            *(int*)(pData + offset) = pass.resourceCreates.Count;
            offset += sizeof(int);
            for (var j = 0; j < pass.resourceCreates.Count; j++)
            {
                *(int*)(pData + offset) = pass.resourceCreates[j].Value;
                offset += sizeof(int);
            }
        }

        // Hash resource descriptors
        for (var i = 0; i < _resources.TextureResourceCount; i++)
        {
            var resource = _resources.GetTextureResourceByIndex(i);

            *(int*)(pData + offset) = resource.Descriptor.Width;
            offset += sizeof(int);

            *(int*)(pData + offset) = resource.Descriptor.Height;
            offset += sizeof(int);

            *(TextureFormat*)(pData + offset) = resource.Descriptor.Format;
            offset += sizeof(TextureFormat);

            *(bool*)(pData + offset) = resource.IsImported;
            offset += sizeof(bool);
        }

        var span = new Span<byte>(pData, offset);
        return XxHash64.HashToUInt64(span);
    }

    /// <summary>
    /// Compiles the render graph by culling unused passes and determining resource lifetimes.
    /// </summary>
    public void Compile()
    {
        if (_compiled)
        {
            return;
        }

#if DEBUG
        var sw = System.Diagnostics.Stopwatch.StartNew();
#endif

        // Step 0: Check cache
        var graphHash = ComputeGraphHash(); // 1321433047288519964

#if DEBUG
        var hashTime = sw.Elapsed.TotalMicroseconds;
#endif

        if (_compilationCache.TryGetCached(graphHash, out var cached))
        {
            // CACHE HIT - restore from cache
#if DEBUG
            Console.WriteLine($"\n[CACHE HIT] Hash: {graphHash:X16} (computed in {hashTime:F2}μs)");
#endif
            RestoreFromCache(cached);
#if DEBUG
            sw.Stop();
            Console.WriteLine($"[CACHE HIT] Total restore time: {sw.Elapsed.TotalMicroseconds:F2}μs");
#endif
            _compiled = true;
            return;
        }

#if DEBUG
        Console.WriteLine($"\n[CACHE MISS] Hash: {graphHash:X16} (computed in {hashTime:F2}μs)");
#endif

        _compiledPasses.Clear();

        // Step 1: Mark passes with side effects (writes to imported resources)
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];

            // Check if this pass writes to any imported textures
            for (var j = 0; j < pass.resourceWrites.Count; j++)
            {
                var writeHandle = pass.resourceWrites[j];
                var resource = _resources.GetResource(writeHandle);
                if (resource.IsImported)
                {
                    pass.hasSideEffects = true;
                    break;
                }
            }
        }

        // Step 2: Cull passes based on dependency analysis
        // Mark all passes as culled initially
        for (var i = 0; i < _passes.Count; i++)
        {
            _passes[i].culled = _passes[i].allowCulling && !_passes[i].hasSideEffects;
        }

        // Step 3: Traverse backwards from passes with side effects
        for (var i = _passes.Count - 1; i >= 0; i--)
        {
            var pass = _passes[i];
            if (!pass.culled)
            {
                UnculDependencies(pass);
            }
        }

        // Step 4: Build final pass list (only non-culled passes)
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            if (!pass.culled)
            {
                _compiledPasses.Add(pass);
            }
        }

        // Step 5: Perform resource aliasing to minimize memory usage
        _aliasingManager.AssignPhysicalResources(_resources, _passes.Count);

        // Step 6: Generate barriers for state transitions and aliasing
        GenerateBarriers();

        // Step 7: Store in cache for future frames
        StoreInCache(graphHash);

        _compiled = true;
    }

    /// <summary>
    /// Restores the render graph state from cached compilation results.
    /// </summary>
    private void RestoreFromCache(CachedCompilation cached)
    {
        // Restore compiled pass list
        _compiledPasses.Clear();
        for (var i = 0; i < cached.CompiledPassIndices.Count; i++)
        {
            var passIndex = cached.CompiledPassIndices[i];
            _compiledPasses.Add(_passes[passIndex]);
        }

        // Restore culling flags
        for (var i = 0; i < _passes.Count && i < cached.PassCulledFlags.Count; i++)
        {
            _passes[i].culled = cached.PassCulledFlags[i];
        }

        // Restore aliasing mappings (need to update ResourceAliasingManager)
        _aliasingManager.RestoreFromCache(cached.LogicalToPhysical, cached.PhysicalResources);

        // Restore barriers (deep copy to avoid shared references)
        _barriers.Clear();
        for (var i = 0; i < cached.Barriers.Count; i++)
        {
            _barriers.Add(cached.Barriers[i]);
        }

        // Restore resource states
        _resourceStates.Clear();
        foreach (var kvp in cached.ResourceStates)
        {
            _resourceStates[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Stores current compilation results in the cache.
    /// </summary>
    private void StoreInCache(ulong graphHash)
    {
        var cacheData = new CachedCompilation();

        // Store compiled pass indices
        for (var i = 0; i < _compiledPasses.Count; i++)
        {
            cacheData.CompiledPassIndices.Add(_compiledPasses[i].index);
        }

        // Store culling flags for all passes
        for (var i = 0; i < _passes.Count; i++)
        {
            cacheData.PassCulledFlags.Add(_passes[i].culled);
        }

        // Store aliasing mappings
        _aliasingManager.StoreToCache(cacheData.LogicalToPhysical, cacheData.PhysicalResources);

        // Store barriers
        for (var i = 0; i < _barriers.Count; i++)
        {
            cacheData.Barriers.Add(_barriers[i]);
        }

        // Store resource states
        foreach (var kvp in _resourceStates)
        {
            cacheData.ResourceStates[kvp.Key] = kvp.Value;
        }

        _compilationCache.Store(graphHash, cacheData);
    }

    /// <summary>
    /// Recursively un-cull passes that a given pass depends on.
    /// </summary>
    private void UnculDependencies(RenderGraphPassBase pass)
    {
        // Un-cull all producers of textures we read
        for (var i = 0; i < pass.resourceReads.Count; i++)
        {
            var readHandle = pass.resourceReads[i];
            var resource = _resources.GetResource(readHandle);

            if (resource.ProducerPass >= 0)
            {
                var producer = _passes[resource.ProducerPass];
                if (producer.culled)
                {
                    producer.culled = false;
                    UnculDependencies(producer);
                }
            }
        }
    }

    /// <summary>
    /// Generates resource barriers for state transitions and aliasing.
    /// </summary>
    private void GenerateBarriers()
    {
        _barriers.Clear();
        _resourceStates.Clear();

#if DEBUG
        Console.WriteLine("\n=== Barrier Generation ===");
#endif

        // Process each compiled pass in order
        for (var passIdx = 0; passIdx < _compiledPasses.Count; passIdx++)
        {
            var pass = _compiledPasses[passIdx];

            // Insert aliasing barriers for resources that reuse physical memory
            InsertAliasingBarriers(pass, passIdx);

            // Insert transition barriers for state changes
            InsertTransitionBarriers(pass, passIdx);
        }

#if DEBUG
        Console.WriteLine($"Total Barriers: {_barriers.Count}");
        Console.WriteLine("==========================\n");
#endif
    }

    /// <summary>
    /// Inserts aliasing barriers when a physical resource is reused.
    /// </summary>
    private void InsertAliasingBarriers(RenderGraphPassBase pass, int passIdx)
    {
        // Check all resources written by this pass
        for (var i = 0; i < pass.resourceWrites.Count; i++)
        {
            var id = pass.resourceWrites[i];
            var resource = _resources.GetResource(id);

            // Skip imported resources
            if (resource.IsImported)
                continue;

            // Check if this is the first use of this logical resource
            if (resource.FirstUsePass == pass.index)
            {
                // Rent the physical resource
                var physicalIndex = _aliasingManager.GetPhysicalResourceIndex(id.Value);
                if (physicalIndex >= 0)
                {
                    var physical = _aliasingManager.GetPhysicalResource(physicalIndex);

                    // If this physical resource has multiple aliased resources,
                    // we need an aliasing barrier when switching between them
                    if (physical != null && physical.AliasedLogicalResources.Count > 1)
                    {
                        // Find the resource that used this physical memory most recently before this pass
                        Identifier<RGResource> resourceBefore = default;
                        var mostRecentLastUse = -1;

                        foreach (var otherLogicalIndex in physical.AliasedLogicalResources)
                        {
                            if (otherLogicalIndex != id.Value)
                            {
                                var otherResource = _resources.GetTextureResourceByIndex(otherLogicalIndex);
                                // Check if this resource finished before our resource starts
                                if (otherResource.LastUsePass < pass.index &&
                                    otherResource.LastUsePass > mostRecentLastUse)
                                {
                                    mostRecentLastUse = otherResource.LastUsePass;
                                    resourceBefore = otherLogicalIndex;
                                }
                            }
                        }

                        // If we found a previous resource, insert aliasing barrier
                        if (mostRecentLastUse >= 0)
                        {
                            var barrier = ResourceBarrier.CreateAliasingBarrier(
                                resourceBefore,
                                id,
                                passIdx
                            );
                            _barriers.Add(barrier);

#if DEBUG
                            Console.WriteLine($"  {barrier}");
#endif
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Inserts transition barriers when a resource changes state.
    /// </summary>
    private void InsertTransitionBarriers(RenderGraphPassBase pass, int passIdx)
    {
        // Process reads (transition to shader resource)
        for (var i = 0; i < pass.resourceReads.Count; i++)
        {
            var handle = pass.resourceReads[i];
            InsertTransitionIfNeeded(handle, ResourceState.ShaderResource, passIdx);
        }

        switch (pass.type)
        {
            case RenderPassType.Raster:
                for (var i = 0; i <= pass.maxColorIndex; i++)
                {
                    var access = pass.colorAccess[i];
                    InsertTransitionIfNeeded(access.id.AsResource(), ResourceState.RenderTarget, passIdx);
                }

                if (pass.depthAccess.id.IsValid)
                {
                    var depthAccess = pass.depthAccess;
                    InsertTransitionIfNeeded(depthAccess.id.AsResource(), ResourceState.DepthWrite, passIdx);
                }

                for (var i = 0; i < pass.randomAccess.Count; i++)
                {
                    InsertTransitionIfNeeded(pass.randomAccess[i], ResourceState.UnorderedAccess, passIdx);
                }

                break;
            case RenderPassType.Compute:
                for (var i = 0; i < pass.resourceWrites.Count; i++)
                {
                    var id = pass.resourceWrites[i];
                    InsertTransitionIfNeeded(id, ResourceState.UnorderedAccess, passIdx);
                }

                break;
        }
    }

    /// <summary>
    /// Inserts a transition barrier if the resource state changes.
    /// </summary>
    private void InsertTransitionIfNeeded(Identifier<RGResource> resource, ResourceState newState, int passIdx)
    {
        if (!_resourceStates.TryGetValue(resource.Value, out var currentState))
        {
            // First time seeing this resource, assume undefined
            currentState = ResourceState.Common;
        }

        if (currentState != newState)
        {
            var barrier = ResourceBarrier.CreateTransitionBarrier(
                resource,
                currentState,
                newState,
                passIdx
            );
            _barriers.Add(barrier);
            _resourceStates[resource.Value] = newState;

#if DEBUG
            Console.WriteLine($"  {barrier}");
#endif
        }
    }

    /// <summary>
    /// Executes all compiled passes.
    /// </summary>
    public void Execute()
    {
        if (!_compiled)
        {
            Compile();
        }

        // Execute each non-culled pass
        var barrierIndex = 0;
        for (var i = 0; i < _compiledPasses.Count; i++)
        {
            var pass = _compiledPasses[i];

            // Execute all barriers for this pass
#if DEBUG
            bool hasBarriers = false;
#endif
            while (barrierIndex < _barriers.Count && _barriers[barrierIndex].PassIndex == i)
            {
#if DEBUG
                if (!hasBarriers)
                {
                    Console.WriteLine($"\n=== Barriers before Pass {i}: {pass.name} ===");
                    hasBarriers = true;
                }

                var barrier = _barriers[barrierIndex];
                if (barrier.Type == BarrierType.Transition)
                {
                    _commandBuffer.ResourceBarrier(
                        barrier.Resource,
                        barrier.StateBefore,
                        barrier.StateAfter
                    );
                }
#endif
                // In a real implementation, you would execute the barrier here:
                // ExecuteBarrier(_barriers[barrierIndex]);

                barrierIndex++;
            }
#if DEBUG
            if (hasBarriers)
            {
                Console.WriteLine("=====================================\n");
            }
#endif

            pass.Execute(_renderContext);
        }
    }
}
