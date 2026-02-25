using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Main render graph class that manages resource allocation and pass execution.
/// </summary>
public sealed class RenderGraph : IDisposable
{
    private readonly IResourceManager _resourceManager;

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

    private readonly RenderGraphBlackboard _blackboard;

    private bool _compiled;

    public RenderGraphBlackboard Blackboard => _blackboard;

    public RenderGraph(IResourceManager resourceManager, IPipelineLibrary pipelineLibrary, IShaderCompiler shaderCompiler)
    {
        _resourceManager = resourceManager;

        _objectPool = new RenderGraphObjectPool();
        _resources = new RenderGraphResourceRegistry(_objectPool);

        _passes = new List<RenderGraphPassBase>(32);
        _compiledPasses = new List<RenderGraphPassBase>(32);
        _nativePasses = new List<NativeRenderPass>(32);

        _builder = new RenderGraphBuilder();
        _aliasingManager = new ResourceAliasingManager(resourceManager.ResourceAllocator, _objectPool);

        _compilationCache = new RenderGraphCompilationCache();

        _context = new RenderGraphContext(
            resourceManager,
            pipelineLibrary,
            shaderCompiler,
            _resources
        );

        _nativePassBuilder = new RenderGraphNativePassBuilder(_objectPool, _resources);
        _compiler = new RenderGraphCompiler(resourceManager, _resources, _aliasingManager, _nativePassBuilder, _compilationCache);
        _executor = new RenderGraphExecutor(resourceManager, _resources, _context);

        _blackboard = new RenderGraphBlackboard();
    }

    /// <summary>
    /// Resets the render graph for a new frame.
    /// </summary>
    public void Reset()
    {
        _blackboard.Clear();
        _resources.Clear();
        _aliasingManager.Clear();
        _compiledBarriers.Clear();

        // Return passes to the pool and reset count
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            pass.Reset(_objectPool);
        }

        _passes.Clear();
        _compiledPasses.Clear();

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
        var r = _resourceManager.ResourceDatabase.GetResourceDescription(texture.AsResource());
        if (r.IsFailure)
        {
            return Identifier<RGTexture>.Invalid;
        }

        var desc = r.Value;
        return _resources.ImportTexture(in desc.TextureDescription, texture, name, clearColor, clearDepth, clearStencil, clearAtFirstUse, discardAtLastUse);
    }

    /// <summary>
    /// Imports an external buffer into the render graph.
    /// </summary>
    /// <param name="buffer">The external buffer handle.</param>
    /// <returns>The identifier of the imported render graph buffer. Invalid if import fails.</returns>
    public Identifier<RGBuffer> ImportBuffer(Handle<GraphicsBuffer> buffer, string name)
    {
        var r = _resourceManager.ResourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            return Identifier<RGBuffer>.Invalid;
        }

        var desc = r.Value;
        return _resources.ImportBuffer(in desc.BufferDescription, buffer, name);
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
    /// </summary>
    public Error Compile(in ViewState viewState)
    {
        if (_compiled)
        {
            return Error.None;
        }

        _resources.ResolveTextureSizes(in viewState);

        var graphHash = RenderGraphHasher.ComputeGraphHash(_passes, _resources);
        var error = _compiler.Compile(in viewState, graphHash, _passes, _compiledPasses, _nativePasses, _compiledBarriers);
        if (error != Error.None)
        {
            return error;
        }

        _compiled = true;
        return Error.None;
    }

    /// <summary>
    /// Executes all compiled passes using native render passes where possible.
    /// </summary>
    public Error Execute(ICommandBuffer cmd)
    {
        if (!_compiled)
        {
            return Error.InvalidState;
        }

        return _executor.Execute(cmd, _compiledPasses, _nativePasses, _compiledBarriers);
    }

    public void Dispose()
    {
        _compiler.Dispose();

        // HACK: Ideally, we should have a Dispose method. But for now, we just reset to release resources.
        Reset();
    }
}
