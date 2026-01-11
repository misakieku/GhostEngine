namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Extension methods to provide a cleaner API for setting render functions.
/// These avoid the need for explicit type annotations in lambdas.
/// </summary>
public static class RenderGraphBuilderExtensions
{
    // Internal helper to cast and set
    private static void SetRasterFunc<TPassData>(this RenderGraphBuilder builder, object pass, Action<TPassData, RasterRenderContext> func)
        where TPassData : class, new()
    {
        if (pass is RenderGraphPass<TPassData> typedPass)
        {
            builder.SetRenderFunc(func);
        }
    }

    private static void SetCompFunc<TPassData>(this RenderGraphBuilder builder, object pass, Action<TPassData, ComputeRenderContext> func, bool async)
        where TPassData : class, new()
    {
        if (pass is RenderGraphPass<TPassData> typedPass)
        {
            builder.SetComputeFunc(func, async);
        }
    }
}
