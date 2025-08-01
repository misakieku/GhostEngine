namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Delegate for pass setup function
/// </summary>
public delegate void PassSetupFunction<TPassData>(ref TPassData data, RenderPassBuilder builder) where TPassData : struct;

/// <summary>
/// Delegate for pass execution function
/// </summary>
public delegate void PassExecuteFunction<TPassData>(ref TPassData data, RenderPassContext context) where TPassData : struct;

/// <summary>
/// Base class for render passes in the render graph
/// </summary>
internal abstract class RenderPass
{
    public string Name
    {
        get;
    }
    public int Index
    {
        get; internal set;
    }
    public List<ResourceAccess> ResourceAccesses
    {
        get;
    }
    public List<int> Dependencies
    {
        get;
    }

    protected RenderPass(string name)
    {
        Name = name;
        ResourceAccesses = new List<ResourceAccess>();
        Dependencies = new List<int>();
    }

    public abstract void Setup(RenderPassBuilder builder);
    public abstract void Execute(RenderPassContext context);
}

/// <summary>
/// Typed render pass implementation
/// </summary>
internal sealed class RenderPass<TPassData> : RenderPass
    where TPassData : struct
{
    private readonly PassSetupFunction<TPassData> _setupFunction;
    private readonly PassExecuteFunction<TPassData> _executeFunction;
    private TPassData _passData;

    public RenderPass(string name, PassSetupFunction<TPassData> setupFunction, PassExecuteFunction<TPassData> executeFunction)
        : base(name)
    {
        _setupFunction = setupFunction;
        _executeFunction = executeFunction;
    }

    public override void Setup(RenderPassBuilder builder)
    {
        _setupFunction(ref _passData, builder);
        ResourceAccesses.AddRange(builder.ResourceAccesses);
    }

    public override void Execute(RenderPassContext context)
    {
        _executeFunction(ref _passData, context);
    }

    public void SetPassData(TPassData passData)
    {
        _passData = passData;
    }
}

/// <summary>
/// Builder for creating render passes
/// </summary>
public sealed class RenderPassCreator<TPassData>
    where TPassData : struct
{
    private readonly RenderGraph _renderGraph;
    private readonly string _passName;
    private PassSetupFunction<TPassData>? _setupFunction;
    private PassExecuteFunction<TPassData>? _executeFunction;

    internal RenderPassCreator(RenderGraph renderGraph, string passName)
    {
        _renderGraph = renderGraph;
        _passName = passName;
    }

    /// <summary>
    /// Set the setup function for the render pass
    /// </summary>
    public RenderPassCreator<TPassData> Setup(PassSetupFunction<TPassData> setupFunction)
    {
        _setupFunction = setupFunction;
        return this;
    }

    /// <summary>
    /// Set the render function for the render pass
    /// </summary>
    public RenderPassCreator<TPassData> SetRenderFunc(PassExecuteFunction<TPassData> executeFunction)
    {
        _executeFunction = executeFunction;
        return this;
    }

    public void Compile()
    {
        if (_setupFunction == null)
            throw new InvalidOperationException($"Setup function not set for pass '{_passName}'");
        if (_executeFunction == null)
            throw new InvalidOperationException($"Execute function not set for pass '{_passName}'");

        var pass = new RenderPass<TPassData>(_passName, _setupFunction, _executeFunction);
        _renderGraph.AddPass(pass);
    }
}
