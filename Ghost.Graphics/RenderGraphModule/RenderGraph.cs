using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.IO.Hashing;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Main render graph class that manages resource allocation and pass execution.
/// </summary>
public sealed class RenderGraph : IDisposable
{
    private readonly IGraphicsEngine _graphicsEngine;

    private readonly RenderGraphObjectPool _objectPool;
    private readonly RenderGraphResourceRegistry _resources;

    private readonly List<RenderGraphPassBase> _passes;
    private readonly List<RenderGraphPassBase> _compiledPasses;
    private readonly List<NativeRenderPass> _nativePasses;

    private readonly RenderGraphBuilder _builder;
    private readonly ResourceAliasingManager _aliasingManager;

    private readonly Dictionary<int, ResourceBarrierData> _resourceStates = new(128);
    private readonly List<ResourceBarrier> _barriers = new(128);

    private readonly RenderGraphCompilationCache _compilationCache = new();
    private readonly RenderGraphContext _context;

    private bool _compiled;
    private Handle<GPUResource> _resourceHeap;
    private ViewState _currentViewState;

    public RenderGraphBlackboard Blackboard
    {
        get;
    }

    public RenderGraph(IGraphicsEngine graphicsEngine)
    {
        _graphicsEngine = graphicsEngine;

        _objectPool = new RenderGraphObjectPool();
        _resources = new RenderGraphResourceRegistry(_objectPool);

        _passes = new List<RenderGraphPassBase>(32);
        _compiledPasses = new List<RenderGraphPassBase>(32);
        _nativePasses = new List<NativeRenderPass>(32);

        _builder = new RenderGraphBuilder();
        _aliasingManager = new ResourceAliasingManager(_objectPool);

        _resourceStates = new Dictionary<int, ResourceBarrierData>(64);
        _barriers = new List<ResourceBarrier>(128);

        _compilationCache = new RenderGraphCompilationCache();

        _resourceHeap = Handle<GPUResource>.Invalid;
        _context = new RenderGraphContext(
            _graphicsEngine.ResourceDatabase,
            _graphicsEngine.PipelineLibrary,
            _graphicsEngine.ShaderCompiler,
            _resources
        );

        Blackboard = new RenderGraphBlackboard();
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
        _resources.Reset();

        // Reset aliasing manager
        _aliasingManager.Reset();

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
    /// <param name="texture">The external texture handle.</param>
    /// <returns>The identifier of the imported render graph texture. Invalid if import fails.</returns>
    public Identifier<RGTexture> ImportTexture(Handle<Texture> texture, string name,
        Color128 clearColor = default, float clearDepth = 1.0f, byte clearStencil = 0,
        bool clearAtFirstUse = true, bool discardAtLastUse = true)
    {
        var r = _graphicsEngine.ResourceDatabase.GetResourceDescription(texture.AsResource());
        if (r.IsFailure)
        {
            return Identifier<RGTexture>.Invalid;
        }

        var desc = r.Value;
        return _resources.ImportTexture(in desc._desc.textureDescription, texture, name, clearColor, clearDepth, clearStencil, clearAtFirstUse, discardAtLastUse);
    }

    /// <summary>
    /// Imports an external buffer into the render graph.
    /// </summary>
    /// <param name="buffer">The external buffer handle.</param>
    /// <returns>The identifier of the imported render graph buffer. Invalid if import fails.</returns>
    public Identifier<RGBuffer> ImportBuffer(Handle<GraphicsBuffer> buffer, string name)
    {
        var r = _graphicsEngine.ResourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            return Identifier<RGBuffer>.Invalid;
        }

        var desc = r.Value;
        return _resources.ImportBuffer(in desc._desc.bufferDescription, buffer, name);
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

    public IUnsafeRenderGraphBuilder AddUnsafeRenderPass<TPassData>(string name, out TPassData passData)
        where TPassData : class, new()
    {
        var renderPass = _objectPool.Rent<UnsafeRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, _objectPool.Rent<TPassData>(), name, RenderPassType.Unsafe);
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

        // Hash imported flag
        *(pData + offset) = resource.isImported ? (byte)1 : (byte)0;
        offset += sizeof(byte);

        // For imported textures, hash the backing resource handle
        if (resource.isImported)
        {
            *(int*)(pData + offset) = resource.backingResource.GetHashCode();
            offset += sizeof(int);
            return offset;
        }

        var desc = resource.rgTextureDesc;

        // Hash format (structural)
        *(TextureFormat*)(pData + offset) = desc.format;
        offset += sizeof(TextureFormat);

        // Hash size mode (structural)
        *(RGTextureSizeMode*)(pData + offset) = desc.sizeMode;
        offset += sizeof(RGTextureSizeMode);

        // Hash size specification based on mode
        if (desc.sizeMode == RGTextureSizeMode.Absolute)
        {
            // Absolute mode: hash actual dimensions
            *(uint*)(pData + offset) = desc.width;
            offset += sizeof(uint);
            *(uint*)(pData + offset) = desc.height;
            offset += sizeof(uint);
        }
        else
        {
            // Relative mode: hash scale factors (NOT resolved dimensions)
            *(float*)(pData + offset) = desc.scaleX;
            offset += sizeof(float);
            *(float*)(pData + offset) = desc.scaleY;
            offset += sizeof(float);
        }

        // Hash other structural properties
        *(TextureDimension*)(pData + offset) = desc.dimension;
        offset += sizeof(TextureDimension);
        *(uint*)(pData + offset) = desc.mipLevels;
        offset += sizeof(uint);
        *(TextureUsage*)(pData + offset) = desc.usage;
        offset += sizeof(TextureUsage);

        *(bool*)(pData + offset) = desc.clearAtFirstUse;
        offset += sizeof(bool);
        *(bool*)(pData + offset) = desc.discardAtLastUse;
        offset += sizeof(bool);

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
    public void Compile(in ViewState viewState)
    {
        if (_compiled)
        {
            return;
        }

        _currentViewState = viewState;

        // Resolve texture sizes before computing hash
        _resources.ResolveTextureSizes(in viewState);

        var graphHash = ComputeGraphHash();
        if (_compilationCache.TryGetCached(graphHash, out var cached))
        {
            // Check if view state changed
            if (!cached.viewState.Equals(viewState))
            {
                // View state changed - re-resolve sizes and recreate GPU resources
                _resources.ResolveTextureSizes(in viewState);
                RecreateResourcesForNewViewState(cached, viewState);
            }
            else
            {
                // Perfect cache hit - restore everything
                RestoreFromCache(cached);
            }

            _compiled = true;
            return;
        }

        _compiledPasses.Clear();

        // Mark passes with side effects (writes to imported resources)
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

        // Cull passes based on dependency analysis
        // Mark all passes as culled initially
        for (var i = 0; i < _passes.Count; i++)
        {
            _passes[i].culled = _passes[i].allowCulling && !_passes[i].hasSideEffects;
        }

        // Traverse backwards from passes with side effects
        for (var i = _passes.Count - 1; i >= 0; i--)
        {
            var pass = _passes[i];
            if (!pass.culled)
            {
                UnculDependencies(pass);
            }
        }

        // Build final pass list (only non-culled passes)
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            if (!pass.culled)
            {
                _compiledPasses.Add(pass);
            }
        }

        _aliasingManager.AssignPhysicalResources(_resources, _passes.Count);
        AllocateResource();

        GenerateAliasingBarriers();
        BuildNativeRenderPasses();
        StoreInCache(graphHash);

        _compiled = true;
    }

    private void AllocateResource()
    {
        if (_resourceHeap.IsValid)
        {
            foreach (var res in _resources.Resources)
            {
                if (res.isImported)
                {
                    continue;
                }

                _graphicsEngine.ResourceDatabase.ReleaseResource(res.backingResource);
            }

            _graphicsEngine.ResourceDatabase.ReleaseResource(_resourceHeap);
        }

        if (_aliasingManager.Heap.size == 0)
        {
            return;
        }

        var allocationDesc = new AllocationDesc
        {
            Size = _aliasingManager.Heap.size + 1024 * 1024,
            Alignment = ResourceHeap.DEFAULT_ALIGNMENT,
            HeapFlags = HeapFlags.AlowBufferAndTexture,
            HeapType = HeapType.Default
        };

        _resourceHeap = _graphicsEngine.ResourceAllocator.Allocate(in allocationDesc, "RenderGraphResourceHeap");

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
                res.backingResource = _graphicsEngine.ResourceAllocator.CreateTexture(in textureDesc, res.name, ops).AsResource();
            }
            else if (res.type == RenderGraphResourceType.Buffer)
            {
                res.backingResource = _graphicsEngine.ResourceAllocator.CreateBuffer(in res.bufferDesc, res.name, ops).AsResource();
            }
            else
            {
                throw new NotSupportedException();
            }

            _compilationCache.UpdateBackingResource(i, res.backingResource);
        }
    }

    /// <summary>
    /// Recreates GPU resources when view state changes but compilation cache is valid.
    /// </summary>
    private void RecreateResourcesForNewViewState(CachedCompilation cached, in ViewState newViewState)
    {
        // Restore compilation results (passes, barriers, aliasing)
        RestoreFromCache(cached);
        AllocateResource();

        cached.viewState = newViewState;
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

        for (var i = 0; i < _resources.ResourceCount; i++)
        {
            var res = _resources.Resources[i];

            if (!res.isImported)
            {
                res.backingResource = cached.backingResources[i];
            }
        }

        BuildNativeRenderPasses();
    }

    /// <summary>
    /// Stores current compilation results in the cache.
    /// </summary>
    private void StoreInCache(ulong graphHash)
    {
        var cacheData = new CachedCompilation();

        // Store view state
        cacheData.viewState = _currentViewState;

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

        for (var i = 0; i < _resources.ResourceCount; i++)
        {
            var res = _resources.Resources[i];
            cacheData.backingResources.Add(res.backingResource);
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
    /// Generates aliasing barriers to synchronize resources sharing memory.
    /// </summary>
    private void GenerateAliasingBarriers()
    {
        _barriers.Clear();
        _resourceStates.Clear();

        // Process each compiled pass in order
        for (var passIdx = 0; passIdx < _compiledPasses.Count; passIdx++)
        {
            var pass = _compiledPasses[passIdx];

            // Insert aliasing barriers for resources that reuse physical memory
            InsertAliasingBarriers(pass, passIdx);
        }
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

                            // If we found a previous resource, insert aliasing barrier (sync update)
                            if (mostRecentLastUse >= 0)
                            {
                                // Aliasing Requirement: Transition to Undefined, Sync with Predecessor
                                var targetState = new ResourceBarrierData(BarrierLayout.Undefined, BarrierAccess.NoAccess, BarrierSync.None);
                                var barrier = ResourceBarrier.CreateAliasing(passIdx, id, resourceBefore, targetState);
                                _barriers.Add(barrier);
                                
                                // Update local tracker so subsequent transitions know it's Undefined
                                _resourceStates[id.Value] = targetState;
                            }
                        }
                    }
                }
            }
        }
    }

    private ResourceBarrierData GetBufferReadBarrierData(Identifier<RGResource> handle, RenderGraphPassBase pass, RenderGraphResourceType resourceType)
    {
        if (resourceType == RenderGraphResourceType.Texture)
        {
            return new ResourceBarrierData(BarrierLayout.ShaderResource, BarrierAccess.ShaderResource, BarrierSync.PixelShading | BarrierSync.NonPixelShading);
        }

        var sync = BarrierSync.PixelShading | BarrierSync.NonPixelShading;
        var access = BarrierAccess.ShaderResource;
        
        var resource = _resources.GetResource(handle);
        if (resource.bufferDesc.Usage.HasFlag(BufferUsage.IndirectArgument))
        {
            sync = BarrierSync.ExecuteIndirect;
            access = BarrierAccess.IndirectArgument;
        }

        return new ResourceBarrierData(BarrierLayout.Undefined, access, sync);
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
                continue;  // Compute/Unsafe passes execute outside native render passes
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
            var access = pass.colorAccess[i];
            nativePass.colorAttachments[i] = new RenderTargetInfo
            {
                texture = access.id,
                access = access.accessFlags
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

            // 1. First use
            if (resource.firstUsePass == nativePass.firstLogicalPass)
            {
                // Clear at first use
                if (resource.rgTextureDesc.clearAtFirstUse)
                {
                    attachment.loadOp = AttachmentLoadOp.Clear;
                    attachment.clearColor = resource.rgTextureDesc.clearColor;
                }
                else
                {
                    attachment.loadOp = AttachmentLoadOp.DontCare;
                }
            }
            // 2. Discard flag: DontCare for performance
            else if (flags.HasFlag(AccessFlags.Discard))
            {
                attachment.loadOp = AttachmentLoadOp.DontCare;
            }
            // 3. Read flag: Must preserve existing contents
            else if (flags.HasFlag(AccessFlags.Read))
            {
                attachment.loadOp = AttachmentLoadOp.Load;
            }
            // 4. Continuation from previous pass
            else
            {
                attachment.loadOp = AttachmentLoadOp.Load;
            }

            // ===== STORE OP INFERENCE =====

            // Last use: No one needs it after this native pass
            if (resource.lastUsePass == nativePass.lastLogicalPass)
            {
                if (resource.rgTextureDesc.discardAtLastUse)
                {
                    attachment.storeOp = AttachmentStoreOp.DontCare;
                }
                else
                {
                    attachment.storeOp = AttachmentStoreOp.Store;
                }
            }
            // Intermediate: Store for future passes
            else
            {
                attachment.storeOp = AttachmentStoreOp.Store;
            }

        }

        // Infer load/store ops for depth attachment
        if (nativePass.hasDepthAttachment)
        {
            ref var attachment = ref nativePass.depthAttachment;
            var resource = _resources.GetResource(attachment.texture);
            var flags = attachment.access;

            // ===== LOAD OP INFERENCE =====

            // 1. First Use
            if (resource.firstUsePass == nativePass.firstLogicalPass)
            {
                // Clear at first use
                if (resource.rgTextureDesc.clearAtFirstUse)
                {
                    attachment.loadOp = AttachmentLoadOp.Clear;
                    attachment.clearDepth = resource.rgTextureDesc.clearDepth;
                    attachment.clearStencil = resource.rgTextureDesc.clearStencil;
                }
                else
                {
                    attachment.loadOp = AttachmentLoadOp.DontCare;
                }
            }
            // 2. Discard flag: DontCare for performance
            else if (flags.HasFlag(AccessFlags.Discard))
            {
                attachment.loadOp = AttachmentLoadOp.DontCare;
            }
            // 3. Read flag: Must preserve existing contents
            else if (flags.HasFlag(AccessFlags.Read))
            {
                attachment.loadOp = AttachmentLoadOp.Load;
            }
            // 4. Continuation from previous pass
            else
            {
                attachment.loadOp = AttachmentLoadOp.Load;
            }

            // ===== STORE OP INFERENCE =====

            // Depth is commonly discarded (depth-only passes, intermediate depth)
            if (resource.lastUsePass == nativePass.lastLogicalPass)
            {
                if (resource.rgTextureDesc.discardAtLastUse)
                {
                    attachment.storeOp = AttachmentStoreOp.DontCare;
                }
                else
                {
                    attachment.storeOp = AttachmentStoreOp.Store;
                }
            }
            else
            {
                attachment.storeOp = AttachmentStoreOp.Store;
            }

        }
    }

    /// <summary>
    /// Executes all compiled passes using native render passes where possible.
    /// </summary>
    public unsafe void Execute(ICommandBuffer cmd)
    {
        if (!_compiled)
        {
            throw new InvalidOperationException("Render graph must be compiled before execution. Call Compile(viewState) first.");
        }

        var barrierIndex = 0;
        var nativePassIndex = 0;
        var logicalPassIndex = 0;

        _context.SetCommandBuffer(cmd);

        var pPassRTDescs = stackalloc PassRenderTargetDesc[8];
        var pRtFormats = stackalloc TextureFormat[8];

        while (logicalPassIndex < _compiledPasses.Count)
        {
            var pass = _compiledPasses[logicalPassIndex];

            // Check if this pass is part of a native render pass
            if (pass.type == RenderPassType.Raster && nativePassIndex < _nativePasses.Count)
            {
                var nativePass = _nativePasses[nativePassIndex];

                // Build barriers for ALL merged passes before beginning the native render pass
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    ExecuteBarriersForPass(cmd, mergedPassIdx, ref barrierIndex);
                }

                // Begin native render pass
                for (var i = 0; i < nativePass.colorAttachmentCount; i++)
                {
                    var attachment = nativePass.colorAttachments[i];
                    pPassRTDescs[i] = new PassRenderTargetDesc
                    {
                        Texture = _resources.GetResource(attachment.texture).backingResource.AsTexture(),
                        ClearColor = attachment.clearColor,
                        LoadOp = attachment.loadOp,
                        StoreOp = attachment.storeOp
                    };
                }

                var depthDesc = new PassDepthStencilDesc
                {
                    Texture = nativePass.hasDepthAttachment
                        ? _resources.GetResource(nativePass.depthAttachment.texture).backingResource.AsTexture()
                        : Handle<Texture>.Invalid,
                    ClearDepth = nativePass.depthAttachment.clearDepth,
                    ClearStencil = nativePass.depthAttachment.clearStencil,
                    DepthLoadOp = nativePass.hasDepthAttachment
                        ? nativePass.depthAttachment.loadOp
                        : AttachmentLoadOp.DontCare,
                    DepthStoreOp = nativePass.hasDepthAttachment
                        ? nativePass.depthAttachment.storeOp
                        : AttachmentStoreOp.DontCare,
                    StencilLoadOp = nativePass.hasDepthAttachment
                        ? nativePass.depthAttachment.loadOp
                        : AttachmentLoadOp.DontCare,
                    StencilStoreOp = nativePass.hasDepthAttachment
                        ? nativePass.depthAttachment.storeOp
                        : AttachmentStoreOp.DontCare
                };

                cmd.BeginRenderPass(new Span<PassRenderTargetDesc>(pPassRTDescs, nativePass.colorAttachmentCount), depthDesc);

                for (var i = 0; i < nativePass.colorAttachmentCount; i++)
                {
                    var attachment = nativePass.colorAttachments[i];
                    var resource = _resources.GetResource(attachment.texture);
                    pRtFormats[i] = resource.rgTextureDesc.format;
                }

                var depthFormat = nativePass.hasDepthAttachment
                    ? _resources.GetResource(nativePass.depthAttachment.texture).rgTextureDesc.format
                    : TextureFormat.Unknown;
                _context.SetRenderTargetFormats(new ReadOnlySpan<TextureFormat>(pRtFormats, nativePass.colorAttachmentCount), depthFormat);

                // Build all merged logical passes within this native render pass
                for (var i = 0; i < nativePass.mergedPassIndices.Count; i++)
                {
                    var mergedPassIdx = nativePass.mergedPassIndices[i];
                    var mergedPass = _compiledPasses[mergedPassIdx];
                    mergedPass.Execute(_context);
                    logicalPassIndex++;
                }

                cmd.EndRenderPass();
                nativePassIndex++;
            }
            else
            {
                // Compute pass or standalone raster pass (not merged) or Unsafe pass
                ExecuteBarriersForPass(cmd, logicalPassIndex, ref barrierIndex);
                pass.Execute(_context);
                logicalPassIndex++;
            }

        }
    }

    /// <summary>
    /// Executes all barriers for a specific pass.
    /// </summary>
    private unsafe void ExecuteBarriersForPass(ICommandBuffer cmd, int passIndex, ref int barrierIndex)
    {
        const int MaxBatch = 64;
        var barriers = stackalloc BarrierDesc[MaxBatch];
        var barrierCount = 0;

        void Flush()
        {
            if (barrierCount > 0)
            {
                cmd.ResourceBarrier(new ReadOnlySpan<BarrierDesc>(barriers, barrierCount));
                barrierCount = 0;
            }
        }

        // 1. Process Aliasing Barriers (Explicitly scheduled)
        while (barrierIndex < _barriers.Count && _barriers[barrierIndex].PassIndex == passIndex)
        {
            var barrierReq = _barriers[barrierIndex++];
            var resourceHandle = _resources.GetResource(barrierReq.Resource).backingResource;

            BarrierLayout layoutBefore;
            BarrierAccess accessBefore;
            BarrierSync syncBefore;

            if (barrierReq.AliasingPredecessor.IsValid)
            {
                var predHandle = _resources.GetResource(barrierReq.AliasingPredecessor).backingResource;
                var predState = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(predHandle).GetValueOrThrow();

                layoutBefore = BarrierLayout.Undefined;
                accessBefore = BarrierAccess.NoAccess;
                syncBefore = predState.Sync;
            }
            else
            {
                var currentState = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(resourceHandle).GetValueOrThrow();
                layoutBefore = currentState.Layout;
                accessBefore = currentState.Access;
                syncBefore = currentState.Sync;
            }

            var target = barrierReq.TargetState;
            var resType = _resources.GetResource(barrierReq.Resource).type;

            BarrierDesc desc;
            if (resType == RenderGraphResourceType.Texture)
            {
                desc = BarrierDesc.Texture(resourceHandle,
                    syncBefore, target.Sync,
                    accessBefore, target.Access,
                    layoutBefore, target.Layout,
                    discard: barrierReq.Flags.HasFlag(BarrierFlags.Discard));
            }
            else
            {
                desc = BarrierDesc.Buffer(resourceHandle,
                    syncBefore, target.Sync,
                    accessBefore, target.Access);
            }

            if (barrierCount >= MaxBatch)
            {
                Flush();
            }

            barriers[barrierCount++] = desc;

            //_graphicsEngine.ResourceDatabase.SetResourceBarrierData(resourceHandle, target);
        }

        // 2. Process Implicit Transitions (Iterate pass resources)
        var pass = _compiledPasses[passIndex];

        // Helper to check and issue transition
        void IssueTransition(Identifier<RGResource> id, ResourceBarrierData target)
        {
            var resource = _resources.GetResource(id);
            var handle = resource.backingResource;
            var current = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(handle).GetValueOrThrow();

            if (current.Layout != target.Layout || current.Access != target.Access || current.Sync != target.Sync)
            {
                BarrierDesc desc;
                if (resource.type == RenderGraphResourceType.Texture)
                {
                    desc = BarrierDesc.Texture(handle,
                        current.Sync, target.Sync,
                        current.Access, target.Access,
                        current.Layout, target.Layout);
                }
                else
                {
                    desc = BarrierDesc.Buffer(handle,
                        current.Sync, target.Sync,
                        current.Access, target.Access);
                }

                if (barrierCount >= MaxBatch) Flush();
                barriers[barrierCount++] = desc;

                //_graphicsEngine.ResourceDatabase.SetResourceBarrierData(handle, target);
            }
        }

        // Reads
        for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
        {
            var readList = pass.resourceReads[i];
            for (var j = 0; j < readList.Count; j++)
            {
                var handle = readList[j];
                var targetState = GetBufferReadBarrierData(handle, pass, (RenderGraphResourceType)i);
                IssueTransition(handle, targetState);
            }
        }

        switch (pass.type)
        {
            case RenderPassType.Raster:
                for (var i = 0; i <= pass.maxColorIndex; i++)
                {
                    if (pass.colorAccess[i].id.IsValid)
                    {
                        var usage = pass.colorAccess[i].usage;
                        var targetState = new ResourceBarrierData(usage.Layout, usage.Access, usage.Sync);
                        IssueTransition(pass.colorAccess[i].id.AsResource(), targetState);
                    }
                }

                if (pass.depthAccess.id.IsValid)
                {
                    var usage = pass.depthAccess.usage;
                    var targetState = new ResourceBarrierData(usage.Layout, usage.Access, usage.Sync);
                    IssueTransition(pass.depthAccess.id.AsResource(), targetState);
                }

                var uavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.AllShading);
                for (var i = 0; i < pass.randomAccess.Count; i++)
                {
                    IssueTransition(pass.randomAccess[i], uavState);
                }
                break;

            case RenderPassType.Compute:
                var computeUavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.ComputeShading);
                for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
                {
                    var writeList = pass.resourceWrites[i];
                    for (var j = 0; j < writeList.Count; j++)
                    {
                        IssueTransition(writeList[j], computeUavState);
                    }
                }
                break;

            case RenderPassType.Unsafe:
                var rtState = new ResourceBarrierData(BarrierLayout.RenderTarget, BarrierAccess.RenderTarget, BarrierSync.RenderTarget);
                for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
                {
                    var writeList = pass.resourceWrites[i];
                    for (var j = 0; j < writeList.Count; j++)
                    {
                        IssueTransition(writeList[j], rtState);
                    }
                }

                var unsafeUavState = new ResourceBarrierData(BarrierLayout.UnorderedAccess, BarrierAccess.UnorderedAccess, BarrierSync.AllShading);
                for (var i = 0; i < pass.randomAccess.Count; i++)
                {
                    IssueTransition(pass.randomAccess[i], unsafeUavState);
                }
                break;
        }

        Flush();
    }

    public void Dispose()
    {
        foreach (var resource in _resources.Resources)
        {
            _graphicsEngine.ResourceDatabase.ReleaseResource(resource.backingResource);
        }

        _graphicsEngine.ResourceDatabase.ReleaseResource(_resourceHeap);

        // We need to reset the whole graph to return resources to the pool
        // FIX: Ideally we should call dispose here for each subsystem
        Reset();
    }
}
