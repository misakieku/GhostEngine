using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Main render graph class that manages heap allocation and pass execution.
/// </summary>
public sealed class RenderGraph : IDisposable
{
    private readonly IResourceDatabase _resourceDatabase;

    private readonly RenderGraphObjectPool _objectPool;
    private readonly RenderGraphResourceRegistry _resources;

    private readonly List<RenderGraphPassBase> _passes;

    private readonly RenderGraphBuilder _builder;

    private readonly RenderGraphCompilationCache _compilationCache;
    private readonly RenderGraphContext _context;

    private readonly RenderGraphCompiler _compiler;
    private readonly RenderGraphExecutor _executor;
    private readonly RenderGraphNativePassBuilder _nativePassBuilder;

    private readonly RenderGraphBlackboard _blackboard;

    public RenderGraphBlackboard Blackboard => _blackboard;

    public RenderGraph(RenderEngine renderSystem)
    {
        _resourceDatabase = renderSystem.GraphicsEngine.ResourceDatabase;

        _objectPool = new RenderGraphObjectPool();
        _resources = new RenderGraphResourceRegistry(_resourceDatabase, renderSystem.GraphicsEngine.ResourceAllocator);

        _passes = new List<RenderGraphPassBase>(32);

        _builder = new RenderGraphBuilder();

        _compilationCache = new RenderGraphCompilationCache();

        _context = new RenderGraphContext(
            renderSystem.ResourceManager,
            renderSystem.ShaderLibrary,
            renderSystem.GraphicsEngine.ResourceDatabase,
            renderSystem.GraphicsEngine.PipelineLibrary,
            _resources
        );

        _nativePassBuilder = new RenderGraphNativePassBuilder(_objectPool, _resources);
        _compiler = new RenderGraphCompiler(renderSystem.GraphicsEngine.ResourceAllocator, _resources, _nativePassBuilder, _compilationCache);
        _executor = new RenderGraphExecutor(renderSystem.ResourceManager, renderSystem.GraphicsEngine.ResourceDatabase, _resources, _context);

        _blackboard = new RenderGraphBlackboard();
    }

    /// <summary>
    /// Resets the render graph for a new frame.
    /// </summary>
    public void Reset()
    {
        _blackboard.Clear();
        _resources.Clear();

        // Return passes to the pool and reset count
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            pass.Reset(_objectPool);
        }

        _passes.Clear();
    }

    /// <summary>
    /// Imports an external texture into the render graph.
    /// </summary>
    /// <param name="texture">The external texture handle.</param>
    /// <returns>The identifier of the imported render graph texture. Invalid if import fails.</returns>
    public Identifier<RGTexture> ImportTexture(Handle<GPUTexture> texture, string name,
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
        return _resources.ImportTexture(in desc.TextureDescriptor, texture, name, clearColor, clearDepth, clearStencil, clearAtFirstUse, discardAtLastUse);
    }

    /// <summary>
    /// Imports an external buffer into the render graph.
    /// </summary>
    /// <param name="buffer">The external buffer handle.</param>
    /// <returns>The identifier of the imported render graph buffer. Invalid if import fails.</returns>
    public Identifier<RGBuffer> ImportBuffer(Handle<GPUBuffer> buffer, string name)
    {
        var r = _resourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            Logger.Error("Failed to get resource description for buffer handle: " + buffer);
            return Identifier<RGBuffer>.Invalid;
        }

        var desc = r.Value;
        return _resources.ImportBuffer(in desc.BufferDescriptor, buffer, name);
    }

    /// <summary>
    /// Add a new raster render pass to the render graph.
    /// </summary>
    /// <remarks>
    /// This pass will be merged into native render pass when possible.
    /// </remarks>
    /// <param name="name">The name of the render pass.</param>
    /// <param name="passData">The data that will be used during rendering.</param>
    /// <returns>The builder to build the render pass,</returns>
    public IRasterRenderGraphBuilder AddRasterRenderPass<TPassData>(string name, out TPassData passData)
        where TPassData : class, new()
    {
        var renderPass = _objectPool.Rent<RasterRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, _objectPool.Rent<TPassData>(), name, RenderPassType.Raster);
        passData = renderPass.passData;

        _passes.Add(renderPass);

        _builder.Reset(renderPass, _resources);
        return _builder;
    }

    /// <summary>
    /// Add a new compute render pass to the render graph.
    /// </summary>
    /// <param name="name">The name of the render pass.</param>
    /// <param name="passData">The data that will be used during rendering.</param>
    /// <returns>The builder to build the render pass,</returns>
    public IComputeRenderGraphBuilder AddComputeRenderPass<TPassData>(string name, out TPassData passData)
        where TPassData : class, new()
    {
        var renderPass = _objectPool.Rent<ComputeRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, _objectPool.Rent<TPassData>(), name, RenderPassType.Compute);
        passData = renderPass.passData;

        _passes.Add(renderPass);

        _builder.Reset(renderPass, _resources);
        return _builder;
    }

    /// <summary>
    /// Add a new unsafe render pass to the render graph.
    /// </summary>
    /// <param name="name">The name of the render pass.</param>
    /// <param name="passData">The data that will be used during rendering.</param>
    /// <returns>The builder to build the render pass,</returns>
    public IUnsafeRenderGraphBuilder AddUnsafeRenderPass<TPassData>(string name, out TPassData passData)
        where TPassData : class, new()
    {
        var renderPass = _objectPool.Rent<UnsafeRenderGraphPass<TPassData>>();
        renderPass.Init(_passes.Count, _objectPool.Rent<TPassData>(), name, RenderPassType.Unsafe);
        passData = renderPass.passData;

        _passes.Add(renderPass);

        _builder.Reset(renderPass, _resources);
        return _builder;
    }

    /// <summary>
    /// Compiles the render graph the execute all compiled passes.
    /// </summary>
    public Error CompileAndExecute(ICommandBuffer commandBuffer, ViewState viewState)
    {
        _resources.ResolveTextureSizes(in viewState);

        using var scope = AllocationManager.CreateStackScope();
        var graphHash = RenderGraphHasher.ComputeGraphHash(_passes, _resources);
        var result = _compiler.Compile(in viewState, graphHash, _passes, scope.AllocationHandle);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var graph = result.Value;
        _context.RelativeScale = graph.scale;
        return _executor.Execute(commandBuffer, graph.compiledPasses, graph.nativePasses, graph.compiledBarriers);

    }

    public void Dispose()
    {
        _resources.Dispose();

        // HACK: Ideally, we should have a Dispose method. But for now, we just reset to release resources.
        Reset();
    }
}
