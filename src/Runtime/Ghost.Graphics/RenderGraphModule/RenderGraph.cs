using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

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

    private MemoryPool<TLSF, TLSF.CreationOptions> _memoryPool;
    private readonly RenderGraphObjectPool _objectPool;
    private readonly RenderGraphResourceRegistry _resourceRegistry;

    private readonly List<RenderGraphPass> _passes;

    private readonly RenderGraphContext _context;

    private readonly RenderGraphCompiler _compiler;
    private readonly RenderGraphExecutor _executor;

    private readonly RenderGraphBlackboard _blackboard;
    private readonly RenderGraphBuilder _builder;

    public RenderGraphBlackboard Blackboard => _blackboard;

    public RenderGraph(IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator, IPipelineLibrary pipelineLibrary, ResourceManager resourceManager, ShaderLibrary shaderLibrary)
    {
        _resourceDatabase = resourceDatabase;
        _resourceManager = resourceManager;

        _memoryPool = new MemoryPool<TLSF, TLSF.CreationOptions>(new TLSF.CreationOptions { alignment = 16, initialChunkSize = 1024 * 1024 * 16 });

        _objectPool = new RenderGraphObjectPool();
        _resourceRegistry = new RenderGraphResourceRegistry(resourceDatabase, resourceAllocator, resourceManager);

        _passes = new List<RenderGraphPass>(32);

        _context = new RenderGraphContext(resourceManager, shaderLibrary, resourceAllocator, resourceDatabase, pipelineLibrary, _resourceRegistry);

        _compiler = new RenderGraphCompiler(resourceAllocator, _resourceRegistry);
        _executor = new RenderGraphExecutor(_resourceRegistry);

        _blackboard = new RenderGraphBlackboard();
        _builder = new RenderGraphBuilder(_resourceRegistry, _blackboard);
    }

    /// <summary>
    /// Begins recording for a new frame with optional active pipeline keywords.
    /// </summary>
    public void BeginRecord(KeywordMask128 activeKeywords = default)
    {
        _blackboard.Reset();
        _resourceRegistry.Reset();

        _context.Reset(activeKeywords);

        // Return passes to the pool and reset count
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            pass.Reset(_objectPool);
        }

        _passes.Clear();
    }

    /// <summary>
    /// Resets the render graph for a new frame.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(KeywordMask128 activeKeywords = default)
    {
        BeginRecord(activeKeywords);
    }

    /// <summary>
    /// Forces the compilation cache to be invalidated.
    /// </summary>
    public void InvalidateCache()
    {
        _compiler.InvalidateCache();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BufferDesc GetBufferDesc(Identifier<RGBuffer> buffer)
    {
        return ref _resourceRegistry.GetResource(buffer).bufferDesc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RGTextureDesc GetTextureDesc(Identifier<RGTexture> texture)
    {
        return ref _resourceRegistry.GetResource(texture).rgTextureDesc;
    }

    /// <summary>
    /// Creates a transient buffer in the render graph.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Identifier<RGBuffer> CreateBuffer(scoped in BufferDesc desc, string? name = null)
    {
        return _resourceRegistry.CreateBuffer(in desc, name);
    }

    /// <summary>
    /// Creates a transient texture in the render graph.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Identifier<RGTexture> CreateTexture(scoped in RGTextureDesc desc, string? name = null)
    {
        return _resourceRegistry.CreateTexture(in desc, name);
    }

    /// <summary>
    /// Imports an external texture into the render graph.
    /// </summary>
    /// <param name="texture">The external texture handle.</param>
    /// <returns>The identifier of the imported render graph texture. Invalid if import fails.</returns>
    public Identifier<RGTexture> ImportTexture(
        Handle<GPUTexture> texture, ResourceBarrierData? initialState = null, ResourceBarrierData? finalState = null,
        Color128 clearColor = default, float clearDepth = 1.0f, byte clearStencil = 0,
        bool clearAtFirstUse = false, bool discardAtLastUse = false)
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
            in desc.TextureDescriptor, texture, name,
            clearColor, clearDepth, clearStencil,
            clearAtFirstUse, discardAtLastUse,
            initialState, finalState);
    }

    /// <summary>
    /// Imports an external buffer into the render graph.
    /// </summary>
    /// <param name="buffer">The external buffer handle.</param>
    /// <returns>The identifier of the imported render graph buffer. Invalid if import fails.</returns>
    public Identifier<RGBuffer> ImportBuffer(Handle<GPUBuffer> buffer, ResourceBarrierData? initialBarrierState = null, ResourceBarrierData? finalBarrierState = null)
    {
        var r = _resourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            Logger.Error("Failed to get resource description for buffer handle: " + buffer);
            return Identifier<RGBuffer>.Invalid;
        }

        var desc = r.Value;
        var name = _resourceDatabase.GetResourceName(buffer.AsResource());
        return _resourceRegistry.ImportBuffer(in desc.BufferDescriptor, buffer, name, initialBarrierState, finalBarrierState);
    }

    /// <summary>
    /// Queues extraction of a transient texture to an external persistent texture handle upon frame execution.
    /// </summary>
    public void QueueTextureExtraction(Identifier<RGTexture> src, Handle<GPUTexture> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.None)
    {
        ref var resource = ref _resourceRegistry.GetResource(src);
        resource.isExtracted = true;
        resource.extractionTarget = dst.AsResource();
        resource.extractionFlags = flags;
    }

    /// <summary>
    /// Queues extraction of a transient buffer to an external persistent buffer handle upon frame execution.
    /// </summary>
    public void QueueBufferExtraction(Identifier<RGBuffer> src, Handle<GPUBuffer> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.None)
    {
        ref var resource = ref _resourceRegistry.GetResource(src);
        resource.isExtracted = true;
        resource.extractionTarget = dst.AsResource();
        resource.extractionFlags = flags;
    }

    /// <summary>
    /// Add a new raster render pass to the render graph.
    /// </summary>
    /// <remarks>
    /// This pass will be merged into native render pass when possible. Viewport and scissor will be set automatically based on the render target size.
    /// </remarks>
    /// <param name="name">The name of the render pass.</param>
    /// <returns>The builder to build the render pass,</returns>
    public IRasterRenderGraphBuilder AddRasterRenderPass<TPassData>(string name)
        where TPassData : struct
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
        where TPassData : struct
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
        where TPassData : struct
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
    // TODO: Maybe we can introduce a new method called BeginRecord and take the ViewPortState as a parameter,
    // When there is a relative texture, we can resolve the actual size directly?
    public Result<RGExecution, Error> CompileAndExecute(in RenderGraphExecutionContext executionContext, ViewportState viewport, RGFlags flags = RGFlags.Default)
    {
        _resourceRegistry.ResolveTextureSizes(in viewport);

        var graphHash = RenderGraphHasher.ComputeGraphHash(_passes, _resourceRegistry);
        var result = _compiler.Compile(in viewport, graphHash, _passes, flags, _memoryPool.AllocationHandle);
        if (result.IsFailure)
        {
            return result.Error;
        }

        using var graph = result.Value;
        _context.RelativeScale = graph.scale;

        var error = _executor.Execute(executionContext, _context, graph, flags, out var graphicsSubmission, out var computeSubmission);
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
            if (src == dst)
            {
                continue;
            }
            if (res.extractionFlags.HasFlag(ResourceExtractionFlags.ReleaseAfterExtract))
            {
                // Deferred Replace: dst keeps its old resource/descriptors until this frame retires, so
                // in-flight passes sampling the persistent handle (e.g. next frame's HZB history read)
                // never observe this frame's not-yet-written transient. The displaced old resource is
                // released through the deferred release path at apply time.
                _resourceDatabase.QueueReplace(dst, src);
            }
            else
            {
                // Deferred Swap & Pool Recycle.
                // Swapping swaps dst and src inside IResourceDatabase once this frame retires.
                // After Swap, src holds dst's OLD resource handle; its return to the pool is deferred
                // through ReleasePooledResourceDeferred (plain data, no closures) so pooled reuse also
                // waits out in-flight readers.
                _resourceDatabase.QueueSwap(dst, src);
                _resourceManager.ReleasePooledResourceDeferred(src);
            }
        }

        var dump = flags.HasFlag(RGFlags.GenerateDump)
            ? RenderGraphDump.Create(_resourceRegistry, _passes, in graph, viewport)
            : null;

        return new RGExecution
        {
            Dump = dump,
            GraphicsSubmission = graphicsSubmission,
            ComputeSubmission = computeSubmission
        };
    }

    public void SetFrameData(Handle<GPUBuffer> frameBuffer)
    {
        _context.SetFrameData(frameBuffer);
    }

    public void SetViewData(Handle<GPUBuffer> viewBuffer)
    {
        _context.SetViewData(viewBuffer);
    }

    public void Dispose()
    {
        _executor.Dispose();
        _compiler.Dispose();
        _context.Dispose();
        _resourceRegistry.Dispose();

        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            pass.Reset(_objectPool);
        }

        _memoryPool.Dispose();
    }
}
