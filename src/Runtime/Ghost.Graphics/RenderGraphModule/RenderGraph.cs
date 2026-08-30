using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.Graphics.RenderGraphModule;

public readonly struct RGExecution
{
    /// <summary>
    /// Gets the optional diagnostic dump generated for this execution.
    /// </summary>
    public RenderGraphDump? Dump
    {
        get; init;
    }

    /// <summary>
    /// Gets the terminal Graphics submission produced by this execution.
    /// </summary>
    public SubmissionHandle GraphicsSubmission
    {
        get; init;
    }

    /// <summary>
    /// Gets the terminal Compute submission produced by this execution, or an invalid handle when no native Compute submission was made.
    /// </summary>
    public SubmissionHandle ComputeSubmission
    {
        get; init;
    }
}

/// <summary>
/// Main render graph class that manages heap allocation and pass execution.
/// </summary>
public sealed class RenderGraph : IDisposable
{
    private readonly IResourceDatabase _resourceDatabase;
    private readonly ResourceManager _resourceManager;

    private readonly RenderGraphObjectPool _objectPool;
    private readonly RenderGraphResourceRegistry _resourceRegistry;

    private readonly List<RenderGraphPass> _passes;

    private readonly RenderGraphContext _context;

    private readonly RenderGraphCompiler _compiler;
    private readonly RenderGraphExecutor _executor;

    private readonly RenderGraphBlackboard _blackboard;
    private readonly RenderGraphBuilder _builder;

    private MemoryPool<TLSF, TLSF.CreationOptions> _memoryPool;

    public RenderGraphBlackboard Blackboard => _blackboard;

    public RenderGraph(IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator, IPipelineLibrary pipelineLibrary, ResourceManager resourceManager, ShaderLibrary shaderLibrary)
    {
        _resourceDatabase = resourceDatabase;
        _resourceManager = resourceManager;

        _objectPool = new RenderGraphObjectPool();
        _resourceRegistry = new RenderGraphResourceRegistry(resourceDatabase, resourceAllocator, resourceManager);

        _passes = new List<RenderGraphPass>(32);

        _context = new RenderGraphContext(
            resourceManager,
            shaderLibrary,
            resourceDatabase,
            pipelineLibrary,
            _resourceRegistry
        );

        _compiler = new RenderGraphCompiler(resourceAllocator, _resourceRegistry);
        _executor = new RenderGraphExecutor(_resourceRegistry);

        _blackboard = new RenderGraphBlackboard();
        _builder = new RenderGraphBuilder(_resourceRegistry, _blackboard);

        _memoryPool = new MemoryPool<TLSF, TLSF.CreationOptions>(new TLSF.CreationOptions { alignment = 16, initialChunkSize = 1024 * 1024 * 16 });
    }

    private RenderGraphDump GenerateDump(scoped in CompiledGraph graph, ViewState viewState)
    {
        var dump = new RenderGraphDump
        {
            GraphHash = graph.graphHash,
            TotalHeapSize = graph.plan.totalHeapSize,
            IsCacheHit = graph.cacheHit,
            ViewState = viewState
        };

        var effectiveQueues = new Dictionary<int, CommandQueueType>();
        var syncBoundariesBefore = new Dictionary<int, PassSyncBoundaryDumpInfo>();
        var syncBoundariesAfter = new Dictionary<int, PassSyncBoundaryDumpInfo>();
        var reader = new SpanReader(graph.commandStream.AsSpan());
        dump.CommandStream.AddRange(DisassembleCommandStream(
            ref reader,
            graph.nativePasses.AsSpan(),
            effectiveQueues,
            syncBoundariesBefore,
            syncBoundariesAfter));

        // Collect Memory Placement Blocks (Heap Blocks)
        var uniqueBlocks = graph.plan.placedResources.AsSpan().ToArray()
            .GroupBy(p => p.heapOffset);

        foreach (var group in uniqueBlocks)
        {
            var maxBlockSize = 0UL;
            var allLogicalIds = new List<int>();

            foreach (var placed in group)
            {
                maxBlockSize = Math.Max(maxBlockSize, placed.sizeInBytes);
                foreach (var id in placed.aliasedLogicalResources)
                {
                    if (!allLogicalIds.Contains(id))
                    {
                        allLogicalIds.Add(id);
                    }
                }
            }

            dump.MemoryBlocks.Add(new HeapBlockDumpInfo
            {
                Offset = group.Key,
                Size = maxBlockSize,
                IsFree = false,
                AliasedLogicalResources = allLogicalIds
            });
        }

        // Collect Passes Info
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            CommandQueueType? effectiveQueue = effectiveQueues.TryGetValue(pass.index, out var queue)
                ? queue
                : null;
            var passInfo = new PassDumpInfo
            {
                Index = pass.index,
                NativePassIndex = graph.nativePasses.FirstOrDefault(np => np.mergedPassIndices.Contains(pass.index), NativeRenderPass.Invalid).index,
                Name = pass.name,
                Type = pass.type,
                IsCulled = pass.culled,
                AsyncCompute = pass.asyncCompute,
                AsyncRequested = pass.asyncCompute,
                EffectiveQueue = effectiveQueue,
                QueueDecision = GetQueueDecision(pass, effectiveQueue),
                SyncBoundaryBefore = syncBoundariesBefore.TryGetValue(pass.index, out var boundaryBefore)
                    ? boundaryBefore
                    : null,
                SyncBoundaryAfter = syncBoundariesAfter.TryGetValue(pass.index, out var boundaryAfter)
                    ? boundaryAfter
                    : null,
                ResourceReads = pass.resourceReads[(int)RGResourceType.Texture]
                    .Concat(pass.resourceReads[(int)RGResourceType.Buffer])
                    .Select(r => r.Value).ToList(),
                ResourceWrites = pass.resourceWrites[(int)RGResourceType.Texture]
                    .Concat(pass.resourceWrites[(int)RGResourceType.Buffer])
                    .Select(r => r.Value).ToList(),
                ResourceCreates = pass.resourceCreates[(int)RGResourceType.Texture]
                    .Concat(pass.resourceCreates[(int)RGResourceType.Buffer])
                    .Select(r => r.Value).ToList(),
            };

            dump.Passes.Add(passInfo);
        }

        // Collect Resource & Aliasing Info
        var plan = graph.plan;
        for (var i = 0; i < _resourceRegistry.ResourceCount; i++)
        {
            ref readonly var res = ref _resourceRegistry.GetResourceByIndex(i);
            var placedIndex = plan.GetPlacedResourceIndex(i);
            var placedResult = plan.GetPlacedResource(placedIndex);
            var resInfo = new ResourceDumpInfo
            {
                LogicalResourceId = res.index,
                BackingResource = res.backingResource,
                Name = _resourceRegistry.GetResourceName(i),
                Type = res.type,
                IsImported = res.isImported,
                IsExtracted = res.isExtracted,
                HeapOffset = placedResult.IsSuccess ? placedResult.Value.heapOffset : 0,
                SizeInBytes = placedResult.IsSuccess ? placedResult.Value.sizeInBytes : 0,
                FirstUsePass = res.firstUsePass,
                LastUsePass = res.lastUsePass,
                ScheduledFirstUseIndex = graph.resourceFirstUseScheduleIndices[i],
                ScheduledLastUseIndex = graph.resourceLastUseScheduleIndices[i],
                ProducerPass = [.. res.producerPasses],
                ConsumerPasses = [.. res.consumerPasses],
                AliasedWithResources = placedResult.IsSuccess
                    ? placedResult.Value.aliasedLogicalResources.ToList()
                    : new List<int>()
            };

            dump.Resources.Add(resInfo);
        }

        return dump;
    }

    private List<string> DisassembleCommandStream(
        ref SpanReader reader,
        ReadOnlySpan<NativeRenderPass> nativePasses,
        Dictionary<int, CommandQueueType> effectiveQueues,
        Dictionary<int, PassSyncBoundaryDumpInfo> syncBoundariesBefore,
        Dictionary<int, PassSyncBoundaryDumpInfo> syncBoundariesAfter)
    {
        var lines = new List<string>();
        var commandBufferTypes = new List<CommandQueueType> { CommandQueueType.Graphics };
        var instIndex = 0;
        var lastPassIndex = -1;
        PassSyncBoundaryDumpInfo? pendingBoundary = null;
        var effectiveQueue = CommandQueueType.Graphics;

        while (reader.RemainingBytes > 0)
        {
            var op = reader.Read<RGExecutionOpType>();

            switch (op)
            {
                case RGExecutionOpType.IssueBarriers:
                {
                    var count = reader.Read<int>();
                    lines.Add($"[{instIndex++:D4}] IssueBarriers ({count} barriers)");
                    for (var i = 0; i < count; i++)
                    {
                        var barrier = reader.Read<CompiledBarrier>();
                        var resourceId = barrier.resource.Value;
                        var resName = _resourceRegistry.GetResourceName(resourceId);
                        var resourceLabel = $"{resName} [{barrier.resourceType} #{resourceId}]";
                        var sourceStateLabel = $"Layout: {barrier.sourceState.layout}, Access: {barrier.sourceState.access}, Sync: {barrier.sourceState.sync}";
                        var handoffStateLabel = $"Layout: {barrier.handoffState.layout}, Access: {barrier.handoffState.access}, Sync: {barrier.handoffState.sync}";
                        var targetStateLabel = $"Layout: {barrier.targetState.layout}, Access: {barrier.targetState.access}, Sync: {barrier.targetState.sync}";
                        if (barrier.flags.HasFlag(BarrierFlags.QueueRelease))
                        {
                            lines.Add(
                                $"       ├─ QueueRelease: {resourceLabel}, {barrier.sourceQueue} -> {barrier.destinationQueue}, " +
                                $"Source: [{sourceStateLabel}], Handoff: [{handoffStateLabel}], Flags: {barrier.flags}");
                        }
                        else if (barrier.flags.HasFlag(BarrierFlags.QueueAcquire))
                        {
                            lines.Add(
                                $"       ├─ QueueAcquire: {resourceLabel}, {barrier.sourceQueue} -> {barrier.destinationQueue}, " +
                                $"Handoff: [{handoffStateLabel}], Target: [{targetStateLabel}], Flags: {barrier.flags}");
                        }
                        else if (barrier.aliasingPredecessor.IsValid)
                        {
                            var predecessorId = barrier.aliasingPredecessor.Value;
                            ref readonly var predecessor = ref _resourceRegistry.GetResource(barrier.aliasingPredecessor);
                            var predName = _resourceRegistry.GetResourceName(predecessorId);
                            var predecessorLabel = $"{predName} [{predecessor.type} #{predecessorId}]";
                            lines.Add($"       ├─ Aliasing: {predecessorLabel} -> {resourceLabel} -> {targetStateLabel}, Flags: {barrier.flags}");
                        }
                        else
                        {
                            lines.Add(
                                $"       ├─ Transition: {resourceLabel}, Source: [{sourceStateLabel}], " +
                                $"Target: [{targetStateLabel}], Flags: {barrier.flags}");
                        }
                    }
                    break;
                }

                case RGExecutionOpType.BeginNativePass:
                {
                    var nativePassIdx = reader.Read<int>();
                    var np = (nativePassIdx >= 0 && nativePassIdx < nativePasses.Length) ? nativePasses[nativePassIdx] : default;
                    lines.Add($"[{instIndex++:D4}] BeginNativePass #{nativePassIdx} (ColorCount: {np.colorAttachmentCount}, HasDepth: {np.hasDepthAttachment})");
                    break;
                }

                case RGExecutionOpType.ExecutePass:
                {
                    var passIdx = reader.Read<int>();
                    var isKnownPass = passIdx >= 0 && passIdx < _passes.Count;
                    var passName = isKnownPass ? _passes[passIdx].name : $"Pass#{passIdx}";
                    var passType = isKnownPass ? _passes[passIdx].type.ToString() : "Unknown";
                    var asyncRequested = isKnownPass && _passes[passIdx].asyncCompute;
                    if (isKnownPass)
                    {
                        effectiveQueues[passIdx] = effectiveQueue;
                        if (pendingBoundary.HasValue)
                        {
                            syncBoundariesBefore[passIdx] = pendingBoundary.Value;
                            pendingBoundary = null;
                        }
                    }
                    lastPassIndex = passIdx;
                    var queueDecision = isKnownPass
                        ? GetQueueDecision(_passes[passIdx], effectiveQueue)
                        : RGQueueDecision.IneligiblePassType;
                    lines.Add(
                        $"[{instIndex++:D4}] ExecutePass #{passIdx} '{passName}' [{passType}] -> " +
                        $"AsyncRequested: {asyncRequested}, EffectiveQueue: {effectiveQueue}, QueueDecision: {queueDecision}");
                    break;
                }

                case RGExecutionOpType.EndNativePass:
                {
                    lines.Add($"[{instIndex++:D4}] EndNativePass");
                    break;
                }

                case RGExecutionOpType.CommandBufferSyncPoint:
                {
                    var marker = RGCommandStream.ReadSyncMarker(ref reader);
                    var producerIds = marker.ProducerCommandBufferIds.ToArray();
                    var producerTypes = new CommandQueueType[producerIds.Length];
                    for (var i = 0; i < producerIds.Length; i++)
                    {
                        producerTypes[i] = commandBufferTypes[producerIds[i]];
                    }

                    var boundary = new PassSyncBoundaryDumpInfo
                    {
                        SourceType = effectiveQueue,
                        DestinationType = marker.NextCommandBufferType,
                        ProducerCommandBufferIds = producerIds,
                        ProducerTypes = producerTypes
                    };
                    if (lastPassIndex >= 0)
                    {
                        syncBoundariesAfter[lastPassIndex] = boundary;
                    }
                    pendingBoundary = boundary;

                    var deps = producerIds.Length > 0 ? string.Join(", ", producerIds) : "none";
                    var dependencyTypes = producerTypes.Length > 0 ? string.Join(", ", producerTypes) : "none";
                    lines.Add(
                        $"[{instIndex++:D4}] CommandBufferSyncPoint -> SourceType: {effectiveQueue}, NextType: {marker.NextCommandBufferType}, " +
                        $"DependsOn: [{deps}], DependencyTypes: [{dependencyTypes}]");
                    effectiveQueue = marker.NextCommandBufferType;
                    commandBufferTypes.Add(effectiveQueue);
                    break;
                }
            }
        }

        return lines;
    }

    private static RGQueueDecision GetQueueDecision(RenderGraphPass pass, CommandQueueType? effectiveQueue)
    {
        if (pass.culled || !effectiveQueue.HasValue)
        {
            return RGQueueDecision.Culled;
        }

        if (pass.type != RenderPassType.Compute)
        {
            return RGQueueDecision.IneligiblePassType;
        }

        if (!pass.asyncCompute)
        {
            return RGQueueDecision.AsyncNotRequested;
        }

        return effectiveQueue == CommandQueueType.Compute
            ? RGQueueDecision.AsyncComputeSelected
            : RGQueueDecision.NoLegalOverlapWindow;
    }

    /// <summary>
    /// Resets the render graph for a new frame.
    /// </summary>
    public void Reset()
    {
        _blackboard.Reset();
        _resourceRegistry.Reset();

        // Return passes to the pool and reset count
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            pass.Reset(_objectPool);
        }

        _passes.Clear();
    }

    /// <summary>
    /// Forces the compilation cache to be invalidated.
    /// </summary>
    public void InvalidateCache()
    {
        _compiler.InvalidateCache();
    }

    /// <summary>
    /// Imports an external texture into the render graph.
    /// </summary>
    /// <param name="texture">The external texture handle.</param>
    /// <returns>The identifier of the imported render graph texture. Invalid if import fails.</returns>
    public Identifier<RGTexture> ImportTexture(
        Handle<GPUTexture> texture,
        ResourceBarrierData? initialState = null,
        ResourceBarrierData? finalState = null,
        Color128 clearColor = default,
        float clearDepth = 1.0f,
        byte clearStencil = 0,
        bool clearAtFirstUse = false,
        bool discardAtLastUse = false)
    {
        var r = _resourceDatabase.GetResourceDescription(texture.AsResource());
        if (r.IsFailure)
        {
            Logger.Error("Failed to get resource description for texture handle: " + texture);
            return Identifier<RGTexture>.Invalid;
        }

        var desc = r.Value;
        var name = _resourceDatabase.GetResourceName(texture.AsResource());
        return _resourceRegistry.ImportTexture(
            in desc.TextureDescriptor,
            texture,
            name,
            clearColor,
            clearDepth,
            clearStencil,
            clearAtFirstUse,
            discardAtLastUse,
            initialState,
            finalState);
    }

    /// <summary>
    /// Imports an external buffer into the render graph.
    /// </summary>
    /// <param name="buffer">The external buffer handle.</param>
    /// <returns>The identifier of the imported render graph buffer. Invalid if import fails.</returns>
    public Identifier<RGBuffer> ImportBuffer(
        Handle<GPUBuffer> buffer,
        ResourceBarrierData? initialBarrierState = null,
        ResourceBarrierData? finalBarrierState = null)
    {
        var r = _resourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            Logger.Error("Failed to get resource description for buffer handle: " + buffer);
            return Identifier<RGBuffer>.Invalid;
        }

        var desc = r.Value;
        var name = _resourceDatabase.GetResourceName(buffer.AsResource());
        return _resourceRegistry.ImportBuffer(
            in desc.BufferDescriptor,
            buffer,
            name,
            initialBarrierState,
            finalBarrierState);
    }

    /// <summary>
    /// Add a new raster render pass to the render graph.
    /// </summary>
    /// <remarks>
    /// This pass will be merged into native render pass when possible.
    /// </remarks>
    /// <param name="name">The name of the render pass.</param>
    /// <returns>The builder to build the render pass,</returns>
    public IRasterRenderGraphBuilder AddRasterRenderPass<TPassData>(string name)
        where TPassData : unmanaged
    {
        var renderPass = _objectPool.Rent<RasterRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, name, RenderPassType.Raster);

        _passes.Add(renderPass);
        _builder.Reset(renderPass);

        return _builder;
    }

    /// <summary>
    /// Add a new compute render pass to the render graph.
    /// </summary>
    /// <param name="name">The name of the render pass.</param>
    /// <param name="passData">The data that will be used during rendering.</param>
    /// <returns>The builder to build the render pass,</returns>
    public IComputeRenderGraphBuilder AddComputeRenderPass<TPassData>(string name)
        where TPassData : unmanaged
    {
        var renderPass = _objectPool.Rent<ComputeRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, name, RenderPassType.Compute);

        _passes.Add(renderPass);
        _builder.Reset(renderPass);

        return _builder;
    }

    /// <summary>
    /// Add a new unsafe render pass to the render graph.
    /// </summary>
    /// <param name="name">The name of the render pass.</param>
    /// <param name="passData">The data that will be used during rendering.</param>
    /// <returns>The builder to build the render pass,</returns>
    public IUnsafeRenderGraphBuilder AddUnsafeRenderPass<TPassData>(string name)
        where TPassData : unmanaged
    {
        var renderPass = _objectPool.Rent<UnsafeRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, name, RenderPassType.Unsafe);

        _passes.Add(renderPass);
        _builder.Reset(renderPass);

        return _builder;
    }

    /// <summary>
    /// Compiles the render graph and executes all compiled passes.
    /// </summary>
    public Result<RGExecution, Error> CompileAndExecute(
        in RenderGraphExecutionContext executionContext,
        ViewState viewState,
        RGExecutionFlags flags = RGExecutionFlags.Default)
    {
        _resourceRegistry.ResolveTextureSizes(in viewState);

        var graphHash = RenderGraphHasher.ComputeGraphHash(_passes, _resourceRegistry);
        var result = _compiler.Compile(in viewState, graphHash, _passes, _memoryPool.AllocationHandle);
        if (result.IsFailure)
        {
            return result.Error;
        }

        using var graph = result.Value;
        _context.RelativeScale = graph.scale;
        var error = _executor.Execute(
            executionContext,
            _context,
            graph,
            flags,
            out var graphicsSubmission,
            out var computeSubmission);

        if (error.IsFailure)
        {
            return error;
        }

        for (var i = 0; i < _resourceRegistry.ResourceCount; i++)
        {
            ref var res = ref _resourceRegistry.GetResourceByIndex(i);
            if (!res.isExtracted || res.extractionTarget.IsInvalid)
            {
                continue;
            }

            var dst = res.extractionTarget;
            var src = res.backingResource;
            if (res.extractionFlags.HasFlag(ResourceExtractionFlags.ReleaseAfterExtract))
            {
                // Direct Replace (releases old dst resource immediately inside DB)
                _resourceDatabase.Replace(dst, src);
            }
            else
            {
                // Swap & Pool Recycle
                // Swapping swaps dst and src inside IResourceDatabase.
                // After Swap, src now holds dst's OLD resource handle, which we return to the pool!
                var err = _resourceDatabase.Swap(dst, src);
                Logger.DebugAssert(err.IsSuccess, "Failed to swap resources in IResourceDatabase: " + err);

                _resourceManager.ReleasePooledResource(src);
            }
        }

        var dump = flags.HasFlag(RGExecutionFlags.GenerateDump)
            ? GenerateDump(graph, viewState)
            : null;

        return new RGExecution
        {
            Dump = dump,
            GraphicsSubmission = graphicsSubmission,
            ComputeSubmission = computeSubmission
        };
    }

    public void Dispose()
    {
        _resourceRegistry.Dispose();
        _compiler.Dispose();

        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            pass.Reset(_objectPool);
        }

        _memoryPool.Dispose();
    }
}
