using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.IO.Hashing;
using System.Threading;

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

    private readonly XxHash64 _hasher = new();

    private int _passCount;
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
        for (var i = 0; i < _passCount; i++)
        {
            var pass = _passes[i];
            pass.Clear();
            _objectPool.Release(pass);
        }
        _passCount = 0;

        // Clear compiled passes list
        _compiledPasses.Clear();
        _compiled = false;
    }

    /// <summary>
    /// Imports an external texture into the render graph.
    /// </summary>
    public RenderGraphTextureHandle ImportTexture(string name, TextureDescriptor descriptor)
    {
        return _resources.ImportTexture(descriptor);
    }

    /// <summary>
    /// Adds a new render pass to the graph.
    /// </summary>
    public RenderGraphBuilder AddRenderPass<TPassData>(string name, out TPassData passData)
        where TPassData : class, new()
    {
        // Get or create pass from pool
        RenderGraphPass<TPassData> pass;
        if (_passCount < _passes.Count)
        {
            // Reuse existing slot
            var existingPass = _passes[_passCount];
            if (existingPass is RenderGraphPass<TPassData> typedPass)
            {
                pass = typedPass;
                pass.Reset();
            }
            else
            {
                // Type mismatch, need to replace
                _objectPool.Release(existingPass);
                pass = _objectPool.Get<RenderGraphPass<TPassData>>();
                pass.Reset();
                _passes[_passCount] = pass;
            }
        }
        else
        {
            // Need to grow the list
            pass = _objectPool.Get<RenderGraphPass<TPassData>>();
            pass.Reset();
            _passes.Add(pass);
        }

        // Initialize pass
        pass.Name = name;
        pass.Index = _passCount;

        // Get or create pass data from pool
        passData = _objectPool.Get<TPassData>();
        pass.PassData = passData;

        _passCount++;

        // Initialize builder
        _builder.Initialize(pass, _resources);
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
        var bufferPool = new UnsafeList<byte>(4096, scope.AllocationHandle);
        int offset = 0;
        var pData = (byte*)bufferPool.GetUnsafePtr();

        _hasher.Reset();

        // Hash pass count
        _hasher.AppendInt(_passCount);

        // Hash each pass structure (excluding names)
        for (int i = 0; i < _passCount; i++)
        {
            var pass = _passes[i];
            // Save 0.004ms.

            //// Hash pass properties that affect compilation
            //_hasher.AppendEnum(pass.Type);
            //_hasher.AppendBool(pass.AllowCulling);
            //_hasher.AppendBool(pass.AsyncCompute);
            
            //// Hash texture dependencies (only indices, not versions or names)
            //_hasher.AppendHandleList(pass.TextureReads);
            //_hasher.AppendHandleList(pass.TextureWrites);
            //_hasher.AppendHandleList(pass.TextureCreates);
            *(RenderPassType*)(pData + offset) = pass.Type;
            offset += sizeof(RenderPassType);

            *(bool*)(pData + offset) = pass.AllowCulling;
            offset += sizeof(bool);

            *(bool*)(pData + offset) = pass.AsyncCompute;
            offset += sizeof(bool);

            *(int*)(pData + offset) = pass.TextureReads.Count;
            offset += sizeof(int);
            for (int j = 0; j < pass.TextureReads.Count; j++)
            {
                *(int*)(pData + offset) = pass.TextureReads[j].Index;
                offset += sizeof(int);
            }

            *(int*)(pData + offset) = pass.TextureWrites.Count;
            offset += sizeof(int);
            for (int j = 0; j < pass.TextureWrites.Count; j++)
            {
                *(int*)(pData + offset) = pass.TextureWrites[j].Index;
                offset += sizeof(int);
            }

            *(int*)(pData + offset) = pass.TextureCreates.Count;
            offset += sizeof(int);
            for (int j = 0; j < pass.TextureCreates.Count; j++)
            {
                *(int*)(pData + offset) = pass.TextureCreates[j].Index;
                offset += sizeof(int);
            }
        }

        // Hash resource descriptors
        for (int i = 0; i < _resources.TextureResourceCount; i++)
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
        _hasher.Append(span);
        return _hasher.GetCurrentHashAsUInt64();
    }

    /// <summary>
    /// Compiles the render graph by culling unused passes and determining resource lifetimes.
    /// </summary>
    public void Compile()
    {
        if (_compiled)
            return;

#if DEBUG
        var sw = System.Diagnostics.Stopwatch.StartNew();
#endif

        // Step 0: Check cache
        ulong graphHash = ComputeGraphHash();

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
        for (var i = 0; i < _passCount; i++)
        {
            var pass = _passes[i];

            // Check if this pass writes to any imported textures
            for (var j = 0; j < pass.TextureWrites.Count; j++)
            {
                var writeHandle = pass.TextureWrites[j];
                var resource = _resources.GetTextureResource(writeHandle);
                if (resource.IsImported)
                {
                    pass.HasSideEffects = true;
                    break;
                }
            }
        }

        // Step 2: Cull passes based on dependency analysis
        // Mark all passes as culled initially
        for (var i = 0; i < _passCount; i++)
        {
            _passes[i].Culled = _passes[i].AllowCulling && !_passes[i].HasSideEffects;
        }

        // Step 3: Traverse backwards from passes with side effects
        for (var i = _passCount - 1; i >= 0; i--)
        {
            var pass = _passes[i];
            if (!pass.Culled)
            {
                UnculDependencies(pass);
            }
        }

        // Step 4: Build final pass list (only non-culled passes)
        for (var i = 0; i < _passCount; i++)
        {
            var pass = _passes[i];
            if (!pass.Culled)
            {
                _compiledPasses.Add(pass);
            }
        }

        // Step 5: Perform resource aliasing to minimize memory usage
        _aliasingManager.AssignPhysicalResources(_resources, _passCount);

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
        for (int i = 0; i < cached.CompiledPassIndices.Count; i++)
        {
            int passIndex = cached.CompiledPassIndices[i];
            _compiledPasses.Add(_passes[passIndex]);
        }

        // Restore culling flags
        for (int i = 0; i < _passCount && i < cached.PassCulledFlags.Count; i++)
        {
            _passes[i].Culled = cached.PassCulledFlags[i];
        }

        // Restore aliasing mappings (need to update ResourceAliasingManager)
        _aliasingManager.RestoreFromCache(cached.LogicalToPhysical, cached.PhysicalResources);

        // Restore barriers (deep copy to avoid shared references)
        _barriers.Clear();
        for (int i = 0; i < cached.Barriers.Count; i++)
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
        for (int i = 0; i < _compiledPasses.Count; i++)
        {
            cacheData.CompiledPassIndices.Add(_compiledPasses[i].Index);
        }

        // Store culling flags for all passes
        for (int i = 0; i < _passCount; i++)
        {
            cacheData.PassCulledFlags.Add(_passes[i].Culled);
        }

        // Store aliasing mappings
        _aliasingManager.StoreToCache(cacheData.LogicalToPhysical, cacheData.PhysicalResources);

        // Store barriers
        for (int i = 0; i < _barriers.Count; i++)
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
        for (var i = 0; i < pass.TextureReads.Count; i++)
        {
            var readHandle = pass.TextureReads[i];
            var resource = _resources.GetTextureResource(readHandle);

            if (resource.ProducerPass >= 0)
            {
                var producer = _passes[resource.ProducerPass];
                if (producer.Culled)
                {
                    producer.Culled = false;
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
        for (int i = 0; i < pass.TextureWrites.Count; i++)
        {
            var handle = pass.TextureWrites[i];
            var resource = _resources.GetTextureResource(handle);
            
            // Skip imported resources
            if (resource.IsImported)
                continue;

            // Check if this is the first use of this logical resource
            if (resource.FirstUsePass == pass.Index)
            {
                // Get the physical resource
                int physicalIndex = _aliasingManager.GetPhysicalResourceIndex(handle.Index);
                if (physicalIndex >= 0)
                {
                    var physical = _aliasingManager.GetPhysicalResource(physicalIndex);
                    
                    // If this physical resource has multiple aliased resources,
                    // we need an aliasing barrier when switching between them
                    if (physical != null && physical.AliasedLogicalResources.Count > 1)
                    {
                        // Find the resource that used this physical memory most recently before this pass
                        RenderGraphTextureHandle resourceBefore = default;
                        int mostRecentLastUse = -1;
                        
                        foreach (int otherLogicalIndex in physical.AliasedLogicalResources)
                        {
                            if (otherLogicalIndex != handle.Index)
                            {
                                var otherResource = _resources.GetTextureResourceByIndex(otherLogicalIndex);
                                // Check if this resource finished before our resource starts
                                if (otherResource.LastUsePass < pass.Index && 
                                    otherResource.LastUsePass > mostRecentLastUse)
                                {
                                    mostRecentLastUse = otherResource.LastUsePass;
                                    resourceBefore = new RenderGraphTextureHandle(
                                        otherLogicalIndex, 
                                        otherResource.Version, 
                                        otherResource.Descriptor.Name);
                                }
                            }
                        }

                        // If we found a previous resource, insert aliasing barrier
                        if (mostRecentLastUse >= 0)
                        {
                            var barrier = ResourceBarrier.CreateAliasingBarrier(
                                resourceBefore,
                                handle,
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
        for (var i = 0; i < pass.TextureReads.Count; i++)
        {
            var handle = pass.TextureReads[i];
            InsertTransitionIfNeeded(handle, ResourceState.ShaderResource, passIdx);
        }

        // Process writes (transition to render target or UAV)
        for (var i = 0; i < pass.TextureWrites.Count; i++)
        {
            var handle = pass.TextureWrites[i];
            var targetState = ResourceState.RenderTarget; // Could be UAV for compute
            InsertTransitionIfNeeded(handle, targetState, passIdx);
        }
    }

    /// <summary>
    /// Inserts a transition barrier if the resource state changes.
    /// </summary>
    private void InsertTransitionIfNeeded(RenderGraphTextureHandle handle, ResourceState newState, int passIdx)
    {
        if (!_resourceStates.TryGetValue(handle.Index, out var currentState))
        {
            // First time seeing this resource, assume undefined
            currentState = ResourceState.Undefined;
        }

        if (currentState != newState)
        {
            var barrier = ResourceBarrier.CreateTransitionBarrier(
                handle,
                currentState,
                newState,
                passIdx
            );
            _barriers.Add(barrier);
            _resourceStates[handle.Index] = newState;

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
        int barrierIndex = 0;
        for (int i = 0; i < _compiledPasses.Count; i++)
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
                    Console.WriteLine($"\n=== Barriers before Pass {i}: {pass.Name} ===");
                    hasBarriers = true;
                }
                Console.WriteLine($"  {_barriers[barrierIndex]}");
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
