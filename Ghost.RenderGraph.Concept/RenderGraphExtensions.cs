namespace Ghost.RenderGraph.Concept;

public static class RenderGraphExtensions
{
    public static RenderGraphPassBuilder<TPassData> AddRenderPass<TPassData>(
        this RenderGraph renderGraph,
        string name,
        out TPassData passData,
        Action<RenderGraphPassBuilder<TPassData>> setup)
        where TPassData : class, new()
    {
        var builder = renderGraph.AddRenderPass<TPassData>(name, out passData);
        setup(builder);
        builder.Dispose();
        return builder;
    }
}

public sealed class RenderGraphPassScope<TPassData> : IDisposable
    where TPassData : class, new()
{
    private readonly RenderGraphPassBuilder<TPassData> _builder;
    private readonly string _passName;

    internal RenderGraphPassScope(RenderGraphPassBuilder<TPassData> builder, string passName)
    {
        _builder = builder;
        _passName = passName;
    }

    public RenderGraphPassBuilder<TPassData> Builder => _builder;

    public void Dispose()
    {
        // Commit the pass when the using block ends
        if (_builder.RenderFunc != null)
        {
            _builder.Dispose();
        }
    }
}
