using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.Graphics.RenderGraphModule;

public readonly struct RGExecution
{
    public RenderGraphDump? Dump
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
    private RenderGraphDump? _cachedDump;

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
        _executor = new RenderGraphExecutor(resourceManager, resourceDatabase, _resourceRegistry);

        _blackboard = new RenderGraphBlackboard();
        _builder = new RenderGraphBuilder(_resourceRegistry, _blackboard);

        _memoryPool = new MemoryPool<TLSF, TLSF.CreationOptions>(new TLSF.CreationOptions { alignment = 16, initialChunkSize = 1024 * 1024 * 16 });
    }

    private RenderGraphDump GenerateDump(scoped in CompiledGraph graph, ViewState viewState)
    {
        var dump = new RenderGraphDump
        {
            TotalHeapSize = graph.plan.totalHeapSize,
            IsCacheHit = graph.cacheHit,
            ViewState = viewState
        };

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
            var passInfo = new PassDumpInfo
            {
                Index = pass.index,
                NativePassIndex = graph.nativePasses.FirstOrDefault(np => np.mergedPassIndices.Contains(pass.index), NativeRenderPass.Invalid).index,
                Name = pass.name,
                Type = pass.type,
                IsCulled = pass.culled,
                AsyncCompute = pass.asyncCompute,
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
                BackingResource = res.backingResource,
                Name = _resourceRegistry.GetResourceName(i),
                Type = res.type,
                IsImported = res.isImported,
                IsExtracted = res.isExtracted,
                HeapOffset = placedResult.IsSuccess ? placedResult.Value.heapOffset : 0,
                SizeInBytes = placedResult.IsSuccess ? placedResult.Value.sizeInBytes : 0,
                FirstUsePass = res.firstUsePass,
                LastUsePass = res.lastUsePass,
                ProducerPass = [..res.producerPasses],
                ConsumerPasses = [..res.consumerPasses],
                AliasedWithResources = placedResult.IsSuccess
                    ? placedResult.Value.aliasedLogicalResources.ToList()
                    : new List<int>()
            };

            dump.Resources.Add(resInfo);
        }

        // Disassemble Command Stream for human-readable debugging
        var reader = new SpanReader(graph.commandStream.AsSpan());
        dump.CommandStream.AddRange(DisassembleCommandStream(ref reader, graph.nativePasses.AsSpan()));

        return dump;
    }

    private List<string> DisassembleCommandStream(ref SpanReader reader, ReadOnlySpan<NativeRenderPass> nativePasses)
    {
        var lines = new List<string>();
        var instIndex = 0;

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
                        var resName = _resourceRegistry.GetResourceName(barrier.resource.Value);
                        if (barrier.aliasingPredecessor.IsValid)
                        {
                            var predName = _resourceRegistry.GetResourceName(barrier.aliasingPredecessor.Value);
                            lines.Add($"       ├─ Aliasing: {predName} -> {resName}");
                        }
                        else
                        {
                            lines.Add($"       ├─ Transition: {resName} -> Layout: {barrier.targetState.layout}, Access: {barrier.targetState.access}, Sync: {barrier.targetState.sync}");
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
                    var passName = (passIdx >= 0 && passIdx < _passes.Count) ? _passes[passIdx].name : $"Pass#{passIdx}";
                    var passType = (passIdx >= 0 && passIdx < _passes.Count) ? _passes[passIdx].type.ToString() : "Unknown";
                    lines.Add($"[{instIndex++:D4}] ExecutePass #{passIdx} '{passName}' [{passType}]");
                    break;
                }

                case RGExecutionOpType.EndNativePass:
                {
                    lines.Add($"[{instIndex++:D4}] EndNativePass");
                    break;
                }

                case RGExecutionOpType.SignalFence:
                {
                    var srcQueue = reader.Read<CommandQueueType>();
                    var fenceVal = reader.Read<ulong>();
                    lines.Add($"[{instIndex++:D4}] SignalFence -> Queue: {srcQueue}, FenceValue: {fenceVal}");
                    break;
                }

                case RGExecutionOpType.SubmitQueue:
                {
                    var targetQueue = reader.Read<CommandQueueType>();
                    lines.Add($"[{instIndex++:D4}] SubmitQueue -> Queue: {targetQueue}");
                    break;
                }

                case RGExecutionOpType.GPUWait:
                {
                    var dstQueue = reader.Read<CommandQueueType>();
                    var fenceVal = reader.Read<ulong>();
                    lines.Add($"[{instIndex++:D4}] GPUWait -> Queue: {dstQueue}, FenceValue: {fenceVal}");
                    break;
                }
            }
        }

        return lines;
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
    public Identifier<RGTexture> ImportTexture(Handle<GPUTexture> texture,
        Color128 clearColor = default, float clearDepth = 1.0f, byte clearStencil = 0,
        bool clearAtFirstUse = true, bool discardAtLastUse = true)
    {
        var r = _resourceDatabase.GetResourceDescription(texture.AsResource());
        if (r.IsFailure)
        {
            Logger.Error("Failed to get resource description for texture handle: " + texture);
            return Identifier<RGTexture>.Invalid;
        }

        var desc = r.Value;
        var name = _resourceDatabase.GetResourceName(texture.AsResource());
        return _resourceRegistry.ImportTexture(in desc.TextureDescriptor, texture, name, clearColor, clearDepth, clearStencil, clearAtFirstUse, discardAtLastUse);
    }

    /// <summary>
    /// Imports an external buffer into the render graph.
    /// </summary>
    /// <param name="buffer">The external buffer handle.</param>
    /// <returns>The identifier of the imported render graph buffer. Invalid if import fails.</returns>
    public Identifier<RGBuffer> ImportBuffer(Handle<GPUBuffer> buffer)
    {
        var r = _resourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            Logger.Error("Failed to get resource description for buffer handle: " + buffer);
            return Identifier<RGBuffer>.Invalid;
        }

        var desc = r.Value;
        var name = _resourceDatabase.GetResourceName(buffer.AsResource());
        return _resourceRegistry.ImportBuffer(in desc.BufferDescriptor, buffer, name);
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
    /// Compiles the render graph and executes all compiled passes with multi-queue support.
    /// </summary>
    public Result<RGExecution, Error> CompileAndExecute(
        ICommandBuffer graphicsCommandBuffer,
        ICommandBuffer computeCommandBuffer,
        ICommandQueue graphicsQueue,
        ICommandQueue computeQueue,
        IFence fence,
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
            graphicsCommandBuffer,
            computeCommandBuffer,
            graphicsQueue,
            computeQueue,
            fence,
            _context,
            graph);

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

        if (flags.HasFlag(RGExecutionFlags.GenerateDump)
            && (_cachedDump == null || _cachedDump.GraphHash != graph.graphHash))
        {
            _cachedDump = GenerateDump(graph, viewState);
        }

        return new RGExecution { Dump = _cachedDump };
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
