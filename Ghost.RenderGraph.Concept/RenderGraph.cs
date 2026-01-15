using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
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
    private readonly List<NativeRenderPass> _nativePasses = new(32);
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
        
        // Return native passes to pool
        for (var i = 0; i < _nativePasses.Count; i++)
        {
            _objectPool.Return(_nativePasses[i]);
        }
        _nativePasses.Clear();
        
        _compiled = false;
    } 

    /// <summary>
    /// Imports an external texture into the render graph.
    /// </summary>
    public Identifier<RGTexture> ImportTexture(TextureDescriptor descriptor)
    {
        return _resources.ImportTexture(descriptor);
    }
    
    /// <summary>
    /// Imports an external buffer into the render graph.
    /// </summary>
    public Identifier<RGBuffer> ImportBuffer(BufferDescriptor descriptor)
    {
        return _resources.ImportBuffer(descriptor);
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

    private unsafe int ComputeTextureHash(byte* pData, int offset, Identifier<RGTexture> texture)
    {
        if (texture.IsInvalid)
        {
            return offset;
        }

        var resource = _resources.GetResource(texture.AsResource());

        // In real implementation, we typically need to handle imported resources differently.

        *(pData + offset) = resource.isImported ? (byte)1 : (byte)0;
        offset += sizeof(byte);

        *(TextureFormat*)(pData + offset) = resource.textureDescriptor.format;
        offset += sizeof(TextureFormat);
        
        *(int*)(pData + offset) = resource.textureDescriptor.width;
        offset += sizeof(int);

        *(int*)(pData + offset) = resource.textureDescriptor.height;
        offset += sizeof(int);

        return offset;
    }

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

            // Hash depth attachment
            offset = ComputeTextureHash(pData, offset, pass.depthAccess.id);

            pData[offset] = (byte)pass.depthAccess.accessFlags;
            offset += sizeof(AccessFlags);

            *(int*)(pData + offset) = pass.maxColorIndex;
            offset += sizeof(int);
            for (var j = 0; j <= pass.maxColorIndex; j++)
            {
                offset = ComputeTextureHash(pData, offset, pass.colorAccess[j].id);

                pData[offset] = (byte)pass.colorAccess[j].accessFlags;
                offset += sizeof(AccessFlags);
            }

            for (var j = 0; j < (int)RenderGraphResourceType.Count; j++)
            {
                var readList = pass.resourceReads[j];
                var writeList = pass.resourceWrites[j];
                var createList = pass.resourceCreates[j];

                *(int*)(pData + offset) = readList.Count;
                offset += sizeof(int);
                for (var k = 0; k < readList.Count; k++)
                {
                    *(int*)(pData + offset) = readList[k].Value;
                    offset += sizeof(int);
                }

                *(int*)(pData + offset) = writeList.Count;
                offset += sizeof(int);
                for (var k = 0; k < writeList.Count; k++)
                {
                    *(int*)(pData + offset) = writeList[k].Value;
                    offset += sizeof(int);
                }

                *(int*)(pData + offset) = createList.Count;
                offset += sizeof(int);
                for (var k = 0; k < createList.Count; k++)
                {
                    *(int*)(pData + offset) = createList[k].Value;
                    offset += sizeof(int);
                }

                *(int*)(pData + offset) = pass.randomAccess.Count;
                offset += sizeof(int);
                for (var k = 0; k < pass.randomAccess.Count; k++)
                {
                    *(int*)(pData + offset) = pass.randomAccess[k].Value;
                    offset += sizeof(int);
                }
                
                // Hash buffer hints (important for correct barrier generation)
                *(int*)(pData + offset) = pass.bufferHints.Count;
                offset += sizeof(int);
                foreach (var kvp in pass.bufferHints)
                {
                    *(int*)(pData + offset) = kvp.Key;  // Buffer resource ID
                    offset += sizeof(int);
                    *(int*)(pData + offset) = (int)kvp.Value;  // BufferHint flags
                    offset += sizeof(int);
                }
            }

            *(int*)(pData + offset) = pass.GetRenderFuncHashCode();
            offset += sizeof(int);
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
        var graphHash = ComputeGraphHash(); // 17020363347016000737

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
        
        // Step 7: Build native render passes by merging compatible passes
        BuildNativeRenderPasses();

        // Step 8: Store in cache for future frames
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
        for (var i = 0; i < cached.compiledPassIndices.Count; i++)
        {
            var passIndex = cached.compiledPassIndices[i];
            _compiledPasses.Add(_passes[passIndex]);
        }

        // Restore culling flags
        for (var i = 0; i < _passes.Count && i < cached.passCulledFlags.Count; i++)
        {
            _passes[i].culled = cached.passCulledFlags[i];
        }

        // Restore aliasing mappings (need to update ResourceAliasingManager)
        _aliasingManager.RestoreFromCache(cached.logicalToPhysical, cached.placedResources);

        // Restore barriers (deep copy to avoid shared references)
        _barriers.Clear();
        for (var i = 0; i < cached.barriers.Count; i++)
        {
            _barriers.Add(cached.barriers[i]);
        }

        // Restore resource states
        _resourceStates.Clear();
        foreach (var kvp in cached.resourceStates)
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
            cacheData.compiledPassIndices.Add(_compiledPasses[i].index);
        }

        // Store culling flags for all passes
        for (var i = 0; i < _passes.Count; i++)
        {
            cacheData.passCulledFlags.Add(_passes[i].culled);
        }

        // Store aliasing mappings
        _aliasingManager.StoreToCache(cacheData.logicalToPhysical, cacheData.placedResources);

        // Store barriers
        for (var i = 0; i < _barriers.Count; i++)
        {
            cacheData.barriers.Add(_barriers[i]);
        }

        // Store resource states
        foreach (var kvp in _resourceStates)
        {
            cacheData.resourceStates[kvp.Key] = kvp.Value;
        }

        _compilationCache.Store(graphHash, cacheData);
    }

    private void UnculProducer(Identifier<RGResource> resource)
    {
        var res = _resources.GetResource(resource);
        if (res.producerPass >= 0)
        {
            var producer = _passes[res.producerPass];
            if (producer.culled)
            {
                producer.culled = false;
                UnculDependencies(producer);
            }
        }
    }

    private void UnculDependencies(RenderGraphPassBase pass)
    {
        // Un-cull producers of read resources
        for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
        {
            var readList = pass.resourceReads[i];
            for (var j = 0; j < readList.Count; j++)
            {
                UnculProducer(readList[j]);
            }
        }

        // Un-cull producers of color attachments
        for (var i = 0; i < pass.maxColorIndex; i++)
        {
            if (pass.colorAccess[i].id.IsValid)
            {
                UnculProducer(pass.colorAccess[i].id.AsResource());
            }
        }

        // Un-cull producer of depth attachment
        if (pass.depthAccess.id.IsValid)
        {
            UnculProducer(pass.depthAccess.id.AsResource());
        }

        // Un-cull producers of UAV resources (if not already in reads/writes)
        for (var i = 0; i < pass.randomAccess.Count; i++)
        {
            UnculProducer(pass.randomAccess[i]);
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
    /// Inserts aliasing barriers when a placed resource is reused.
    /// </summary>
    private void InsertAliasingBarriers(RenderGraphPassBase pass, int passIdx)
    {
        // Check all resources written by this pass (both textures and buffers)
        for (var resType = 0; resType < (int)RenderGraphResourceType.Count; resType++)
        {
            var writeList = pass.resourceWrites[resType];
            for (var i = 0; i < writeList.Count; i++)
            {
                var id = writeList[i];
                var resource = _resources.GetResource(id);

                // Skip imported resources
                if (resource.isImported)
                {
                    continue;
                }

                // Check if this is the first use of this logical resource
                if (resource.firstUsePass == pass.index)
                {
                    // Get the placed resource
                    var placedIndex = _aliasingManager.GetPlacedResourceIndex(id.Value);
                    if (placedIndex >= 0)
                    {
                        var placed = _aliasingManager.GetPlacedResource(placedIndex);

                        // If this placed resource has multiple aliased resources,
                        // we need an aliasing barrier when switching between them
                        if (placed != null && placed.aliasedLogicalResources.Count > 1)
                        {
                            // Find the resource that used this placed memory most recently before this pass
                            Identifier<RGResource> resourceBefore = default;
                            var mostRecentLastUse = -1;

                            foreach (var otherLogicalIndex in placed.aliasedLogicalResources)
                            {
                                if (otherLogicalIndex != id.Value)
                                {
                                    // Get resource by global index
                                    var otherResource = _resources.GetResourceByIndex(otherLogicalIndex);
                                    
                                    // Check if this resource finished before our resource starts
                                    if (otherResource.lastUsePass < pass.index &&
                                        otherResource.lastUsePass > mostRecentLastUse)
                                    {
                                        mostRecentLastUse = otherResource.lastUsePass;
                                        resourceBefore = new Identifier<RGResource>(otherLogicalIndex);
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
    }

    /// <summary>
    /// Inserts transition barriers when a resource changes state.
    /// </summary>
    private void InsertTransitionBarriers(RenderGraphPassBase pass, int passIdx)
    {
        // Process reads (transition to appropriate state based on resource type and hints)
        for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
        {
            var readList = pass.resourceReads[i];
            for (var j = 0; j < readList.Count; j++)
            {
                var handle = readList[j];
                var state = GetBufferReadState(handle, pass, (RenderGraphResourceType)i);
                InsertTransitionIfNeeded(handle, state, passIdx);
            }
        }

        switch (pass.type)
        {
            case RenderPassType.Raster:
                for (var i = 0; i < pass.maxColorIndex; i++)
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
                for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
                {
                    var writeList = pass.resourceWrites[i];
                    for (var j = 0; j < writeList.Count; j++)
                    {
                        var id = writeList[j];
                        InsertTransitionIfNeeded(id, ResourceState.UnorderedAccess, passIdx);
                    }
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
    /// Determines the appropriate resource state for a buffer read operation based on usage hints.
    /// </summary>
    private ResourceState GetBufferReadState(Identifier<RGResource> handle, RenderGraphPassBase pass, RenderGraphResourceType resourceType)
    {
        // Textures always use ShaderResource state
        if (resourceType == RenderGraphResourceType.Texture)
        {
            return ResourceState.ShaderResource;
        }

        // Check for buffer-specific usage hints
        if (pass.bufferHints.TryGetValue(handle.Value, out var hint))
        {
            if (hint.HasFlag(BufferHint.IndirectArgument))
            {
                return ResourceState.IndirectArgument;
            }
        }

        // Default: ByteAddressBuffer read (SRV) - matches bindless architecture
        return ResourceState.ShaderResource;
    }

    /// <summary>
    /// Builds native render passes by merging compatible consecutive raster passes.
    /// Uses conservative merging: only merge passes with identical attachments and no barriers between them.
    /// </summary>
    private void BuildNativeRenderPasses()
    {
        // Clear previous native passes
        for (var i = 0; i < _nativePasses.Count; i++)
        {
            _objectPool.Return(_nativePasses[i]);
        }
        _nativePasses.Clear();

        NativeRenderPass? currentNativePass = null;
        
        for (var i = 0; i < _compiledPasses.Count; i++)
        {
            var pass = _compiledPasses[i];
            
            // Only raster passes can be merged into native render passes
            // Compute passes break the current native render pass
            if (pass.type != RenderPassType.Raster)
            {
                // Close current native pass if open
                if (currentNativePass != null)
                {
                    _nativePasses.Add(currentNativePass);
                    currentNativePass = null;
                }
                continue;  // Compute passes execute outside native render passes
            }
            
            // Check if we can merge with current native pass
            if (currentNativePass != null && CanMergePasses(currentNativePass, pass, i))
            {
                // Merge into existing native pass
                currentNativePass.mergedPassIndices.Add(i);
                currentNativePass.lastLogicalPass = i;
            }
            else
            {
                // Start new native pass
                if (currentNativePass != null)
                {
                    _nativePasses.Add(currentNativePass);
                }
                
                currentNativePass = CreateNativePass(pass, i);
            }
        }
        
        // Add final native pass
        if (currentNativePass != null)
        {
            _nativePasses.Add(currentNativePass);
        }
        
        // Infer load/store operations for all native passes
        for (var i = 0; i < _nativePasses.Count; i++)
        {
            InferLoadStoreOps(_nativePasses[i]);
        }
        
#if DEBUG
        Console.WriteLine("\n=== Native Render Passes ===");
        Console.WriteLine($"Logical passes: {_compiledPasses.Count}");
        Console.WriteLine($"Native passes: {_nativePasses.Count}");
        for (var i = 0; i < _nativePasses.Count; i++)
        {
            var nativePass = _nativePasses[i];
            Console.WriteLine($"\nNative Pass {i}:");
            Console.WriteLine($"  Merged passes: [{string.Join(", ", nativePass.mergedPassIndices)}]");
            Console.WriteLine($"  Color attachments: {nativePass.colorAttachmentCount}");
            for (var j = 0; j < nativePass.colorAttachmentCount; j++)
            {
                Console.WriteLine($"    [{j}] {nativePass.colorAttachments[j].texture}");
            }
            if (nativePass.hasDepthAttachment)
            {
                Console.WriteLine($"  Depth attachment: {nativePass.depthAttachment.texture}");
            }
        }
        Console.WriteLine("============================\n");
#endif
    }

    /// <summary>
    /// Creates a new native render pass from a logical pass.
    /// </summary>
    private NativeRenderPass CreateNativePass(RenderGraphPassBase pass, int passIndex)
    {
        var nativePass = _objectPool.Rent<NativeRenderPass>();
        nativePass.Reset();
        
        nativePass.index = _nativePasses.Count;
        nativePass.mergedPassIndices.Add(passIndex);
        nativePass.firstLogicalPass = passIndex;
        nativePass.lastLogicalPass = passIndex;
        nativePass.allowUAVWrites = pass.randomAccess.Count > 0;
        
        // Copy color attachments
        nativePass.colorAttachmentCount = pass.maxColorIndex + 1;
        for (var i = 0; i <= pass.maxColorIndex; i++)
        {
            nativePass.colorAttachments[i] = new RenderTargetInfo
            {
                texture = pass.colorAccess[i].id,
                access = pass.colorAccess[i].accessFlags
            };
        }
        
        // Copy depth attachment
        if (!pass.depthAccess.id.IsInvalid)
        {
            nativePass.hasDepthAttachment = true;
            nativePass.depthAttachment = new DepthStencilInfo
            {
                texture = pass.depthAccess.id,
                access = pass.depthAccess.accessFlags
            };
        }
        
        return nativePass;
    }

    /// <summary>
    /// Checks if a logical pass can be merged into an existing native render pass.
    /// Conservative merging: only merge if attachments match and no barriers needed.
    /// </summary>
    private bool CanMergePasses(NativeRenderPass nativePass, RenderGraphPassBase pass, int passIndex)
    {
        // Don't merge if UAVs are involved (conservative)
        if (pass.randomAccess.Count > 0 || nativePass.allowUAVWrites)
        {
            return false;
        }
        
        // Check if attachment configuration matches
        if (!AttachmentsMatch(nativePass, pass))
        {
            return false;
        }
        
        // Check if barriers are needed between last merged pass and this pass
        if (RequiresBarrierBetweenPasses(nativePass.lastLogicalPass, passIndex))
        {
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Checks if the attachment configuration of a pass matches the native pass.
    /// </summary>
    private static bool AttachmentsMatch(NativeRenderPass nativePass, RenderGraphPassBase pass)
    {
        // Check color attachment count
        if (nativePass.colorAttachmentCount != pass.maxColorIndex + 1)
        {
            return false;
        }
        
        // Check each color attachment
        for (var i = 0; i < nativePass.colorAttachmentCount; i++)
        {
            if (nativePass.colorAttachments[i].texture != pass.colorAccess[i].id)
            {
                return false;
            }
        }
        
        // Check depth attachment
        if (nativePass.hasDepthAttachment != !pass.depthAccess.id.IsInvalid)
        {
            return false;
        }
        
        if (nativePass.hasDepthAttachment && nativePass.depthAttachment.texture != pass.depthAccess.id)
        {
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Checks if any barriers are required between two passes that would prevent merging.
    /// Only barriers affecting render targets prevent merging; SRV barriers are fine.
    /// </summary>
    private bool RequiresBarrierBetweenPasses(int passA, int passB)
    {
        var laterPass = _compiledPasses[passB];
        
        // Build a set of render target resource IDs (color + depth)
        var renderTargets = new HashSet<Identifier<RGResource>>();
        for (var i = 0; i <= laterPass.maxColorIndex; i++)
        {
            if (!laterPass.colorAccess[i].id.IsInvalid)
            {
                renderTargets.Add(laterPass.colorAccess[i].id.AsResource());
            }
        }
        if (!laterPass.depthAccess.id.IsInvalid)
        {
            renderTargets.Add(laterPass.depthAccess.id.AsResource());
        }
        
        // Check if any barriers for passB affect render targets
        for (var i = 0; i < _barriers.Count; i++)
        {
            if (_barriers[i].PassIndex == passB)
            {
                // Only prevent merge if barrier affects a render target
                if (renderTargets.Contains(_barriers[i].Resource))
                {
                    return true;  // Barrier affects render target, cannot merge
                }
            }
            if (_barriers[i].PassIndex > passB)
            {
                break;  // No more barriers for this pass
            }
        }
        
        return false;
    }

    /// <summary>
    /// Infers optimal load/store operations for all attachments in a native render pass.
    /// Uses resource lifetime information to minimize memory bandwidth (critical for TBDR GPUs).
    /// </summary>
    private void InferLoadStoreOps(NativeRenderPass nativePass)
    {
        // Infer load/store ops for color attachments
        for (var i = 0; i < nativePass.colorAttachmentCount; i++)
        {
            ref var attachment = ref nativePass.colorAttachments[i];
            var resource = _resources.GetResource(attachment.texture);
            var flags = attachment.access;
            
            // ===== LOAD OP INFERENCE =====
            
            // 1. WriteAll (Write | Discard): User guarantees full overwrite
            if (flags.HasFlag(AccessFlags.Discard))
            {
                attachment.loadOp = AttachmentLoadOp.DontCare;
#if DEBUG
                Console.WriteLine($"    Color[{i}] LoadOp=DontCare (WriteAll/Discard flag)");
#endif
            }
            // 2. Read: Needs existing contents (e.g., blending)
            else if (flags.HasFlag(AccessFlags.Read))
            {
                attachment.loadOp = AttachmentLoadOp.Load;
#if DEBUG
                Console.WriteLine($"    Color[{i}] LoadOp=Load (Read flag - blending)");
#endif
            }
            // 3. First use: Could use DontCare, but user didn't specify Discard flag
            //    Conservative: use Load to avoid bugs
            else if (resource.firstUsePass == nativePass.firstLogicalPass)
            {
                attachment.loadOp = AttachmentLoadOp.Load;
#if DEBUG
                Console.WriteLine($"    Color[{i}] LoadOp=Load (first use, Write flag - conservative)");
#endif
            }
            // 4. Continuation from previous pass
            else
            {
                attachment.loadOp = AttachmentLoadOp.Load;
#if DEBUG
                Console.WriteLine($"    Color[{i}] LoadOp=Load (continuation from previous pass)");
#endif
            }
            
            // ===== STORE OP INFERENCE =====
            
            // Last use: No one needs it after this native pass
            if (resource.lastUsePass == nativePass.lastLogicalPass)
            {
                attachment.storeOp = AttachmentStoreOp.DontCare;
#if DEBUG
                Console.WriteLine($"    Color[{i}] StoreOp=DontCare (last use - discard)");
#endif
            }
            // Intermediate: Store for future passes
            else
            {
                attachment.storeOp = AttachmentStoreOp.Store;
#if DEBUG
                Console.WriteLine($"    Color[{i}] StoreOp=Store (used by later passes)");
#endif
            }
        }
        
        // Infer load/store ops for depth attachment
        if (nativePass.hasDepthAttachment)
        {
            ref var attachment = ref nativePass.depthAttachment;
            var resource = _resources.GetResource(attachment.texture);
            var flags = attachment.access;
            
            // ===== LOAD OP INFERENCE =====
            
            if (flags.HasFlag(AccessFlags.Discard))
            {
                attachment.loadOp = AttachmentLoadOp.DontCare;
#if DEBUG
                Console.WriteLine($"    Depth LoadOp=DontCare (WriteAll/Discard flag)");
#endif
            }
            else if (flags.HasFlag(AccessFlags.Read))
            {
                attachment.loadOp = AttachmentLoadOp.Load;
#if DEBUG
                Console.WriteLine($"    Depth LoadOp=Load (Read flag)");
#endif
            }
            else if (resource.firstUsePass == nativePass.firstLogicalPass)
            {
                attachment.loadOp = AttachmentLoadOp.Load;
#if DEBUG
                Console.WriteLine($"    Depth LoadOp=Load (first use, Write flag - conservative)");
#endif
            }
            else
            {
                attachment.loadOp = AttachmentLoadOp.Load;
#if DEBUG
                Console.WriteLine($"    Depth LoadOp=Load (continuation)");
#endif
            }
            
            // ===== STORE OP INFERENCE =====
            
            // Depth is commonly discarded (depth-only passes, intermediate depth)
            if (resource.lastUsePass == nativePass.lastLogicalPass)
            {
                attachment.storeOp = AttachmentStoreOp.DontCare;
#if DEBUG
                Console.WriteLine($"    Depth StoreOp=DontCare (last use)");
#endif
            }
            else
            {
                attachment.storeOp = AttachmentStoreOp.Store;
#if DEBUG
                Console.WriteLine($"    Depth StoreOp=Store (used later)");
#endif
            }
        }
    }

    /// <summary>
    /// Executes all compiled passes using native render passes where possible.
    /// </summary>
    public void Execute()
    {
        if (!_compiled)
        {
            Compile();
        }

        var barrierIndex = 0;
        var nativePassIndex = 0;
        var logicalPassIndex = 0;
        
        while (logicalPassIndex < _compiledPasses.Count)
        {
            var pass = _compiledPasses[logicalPassIndex];
            
            // Check if this pass is part of a native render pass
            if (pass.type == RenderPassType.Raster && nativePassIndex < _nativePasses.Count)
            {
                var nativePass = _nativePasses[nativePassIndex];
                
                // Execute barriers for ALL merged passes before beginning the native render pass
                foreach (var mergedPassIdx in nativePass.mergedPassIndices)
                {
                    ExecuteBarriersForPass(mergedPassIdx, ref barrierIndex);
                }
                
                // Begin native render pass
                _commandBuffer.BeginRenderPass(
                    nativePass.index,
                    nativePass.colorAttachmentCount,
                    nativePass.hasDepthAttachment
                );

                // Execute all merged logical passes within this native render pass
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    var mergedPass = _compiledPasses[mergedPassIdx];
                    
#if DEBUG
                    Console.WriteLine($"\n--- Executing Pass {mergedPassIdx}: {mergedPass.name} (in Native Pass {nativePass.index}) ---");
#endif
                    
                    mergedPass.Execute(_renderContext);
                    logicalPassIndex++;
                }
                
                // End native render pass
                _commandBuffer.EndRenderPass();
                
                nativePassIndex++;
            }
            else
            {
                // Compute pass or standalone raster pass (not merged)
                ExecuteBarriersForPass(logicalPassIndex, ref barrierIndex);
                
#if DEBUG
                Console.WriteLine($"\n--- Executing Pass {logicalPassIndex}: {pass.name} (Standalone) ---");
#endif
                
                pass.Execute(_renderContext);
                logicalPassIndex++;
            }
        }
    }

    /// <summary>
    /// Executes all barriers for a specific pass.
    /// </summary>
    private void ExecuteBarriersForPass(int passIndex, ref int barrierIndex)
    {
#if DEBUG
        bool hasBarriers = false;
#endif
        while (barrierIndex < _barriers.Count && _barriers[barrierIndex].PassIndex == passIndex)
        {
#if DEBUG
            if (!hasBarriers)
            {
                var pass = _compiledPasses[passIndex];
                Console.WriteLine($"\n=== Barriers before Pass {passIndex}: {pass.name} ===");
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
            else if (barrier.Type == BarrierType.Aliasing)
            {
                _commandBuffer.AliasBarrier(
                    barrier.ResourceBefore,
                    barrier.ResourceAfter
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
    }
}
