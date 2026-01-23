using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Main render graph class that manages resource allocation and pass execution.
/// Delegates complex operations to specialized components for better organization.
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

    private readonly List<CompiledBarrier> _compiledBarriers = new(128);

    private readonly RenderGraphCompilationCache _compilationCache = new();
    private readonly RenderGraphContext _context;

    private readonly RenderGraphCompiler _compiler;
    private readonly RenderGraphExecutor _executor;
    private readonly RenderGraphNativePassBuilder _nativePassBuilder;

    private bool _compiled;

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
        _aliasingManager = new ResourceAliasingManager(graphicsEngine.ResourceAllocator, _objectPool);

        _compilationCache = new RenderGraphCompilationCache();

        _context = new RenderGraphContext(
            _graphicsEngine.ResourceDatabase,
            _graphicsEngine.PipelineLibrary,
            _graphicsEngine.ShaderCompiler,
            _resources
        );

        _nativePassBuilder = new RenderGraphNativePassBuilder(_objectPool, _resources);
        _compiler = new RenderGraphCompiler(_graphicsEngine, _resources, _aliasingManager, _nativePassBuilder, _compilationCache);
        _executor = new RenderGraphExecutor(_graphicsEngine, _resources, _context);

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

        // Clear compiled barriers
        _compiledBarriers.Clear();

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

    /// <summary>
    /// Compiles the render graph by culling unused passes and determining resource lifetimes.
    /// Delegates to RenderGraphCompiler for the actual compilation work.
    /// </summary>
    public void Compile(in ViewState viewState)
    {
        if (_compiled)
        {
            return;
        }

        // Resolve texture sizes before computing hash
        _resources.ResolveTextureSizes(in viewState);

        // Compute structural hash for caching
        var graphHash = RenderGraphHasher.ComputeGraphHash(_passes, _resources);
        
        // Delegate to compiler
        _compiler.Compile(in viewState, graphHash, _passes, _compiledPasses, _nativePasses, _compiledBarriers);
        _compiled = true;
    }

    /// <summary>
    /// Executes all compiled passes using native render passes where possible.
    /// Delegates to RenderGraphExecutor for the actual execution work.
    /// </summary>
    public void Execute(ICommandBuffer cmd)
    {
        if (!_compiled)
        {
            throw new InvalidOperationException("Render graph must be compiled before execution. Call Compile(viewState) first.");
        }

        _executor.Execute(cmd, _compiledPasses, _nativePasses, _compiledBarriers);
    }

    public void Dispose()
    {
        _compiler.Dispose();

        // We need to reset the whole graph to return resources to the pool
        Reset();
    }
}
