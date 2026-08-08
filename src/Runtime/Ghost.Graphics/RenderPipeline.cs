using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics;

public interface IRenderPayload : IDisposable
{
    ReadOnlySpan<RenderRequest> RenderRequests { get; }

    void AddRenderRequest(scoped in RenderRequest renderRequest);
    void Reset();
}

public interface IRenderPipelineSettings
{
    IRenderPipeline CreatePipeline(RenderEngine renderEngine);
    IRenderPayload CreatePayload(RenderEngine renderEngine, IRenderPipeline renderPipeline);
}

public interface IRenderPipeline : IDisposable
{
    /// <summary>
    /// Records pre-graph commands into the open <see cref="RenderContext.CommandBuffer"/> (the frame prelude).
    /// The command buffer must be open when this is called; the outer frame owns Begin, End, and submission.
    /// </summary>
    void RecordPrelude(RenderContext ctx, int frameIndex, IRenderPayload payload);

    /// <summary>
    /// Compiles and executes the render graph, submitting its native command buffers through the
    /// frame scheduler embedded in <paramref name="executionContext"/>.
    /// </summary>
    /// <returns>
    /// Terminal submission handles the outer frame uses to declare post-graph dependencies
    /// (e.g. Compute → epilogue). Returns <c>default</c> when the graph is empty or execution fails.
    /// </returns>
    RGExecution ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload,
        in RenderGraphExecutionContext executionContext);
}

public readonly ref struct RenderViewData : IDisposable
{
    private readonly ref readonly RenderRequest _request;
    private readonly SwapChainManager _swapChainManager;

    private readonly Handle<GPUTexture> _colorTexture;
    private readonly uint2 _screenSize;

    public readonly ref readonly RenderRequest Request => ref _request;
    public readonly Handle<GPUTexture> ColorTexture => _colorTexture;
    public readonly uint2 ScreenSize => _screenSize;

    public RenderViewData(SwapChainManager swapChainManager, IResourceDatabase resourceDatabase, ref readonly RenderRequest request)
    {
        _request = ref request;
        _swapChainManager = swapChainManager;

        if (request.swapChainIndex < 0)
        {
            _colorTexture = request.colorTarget;
            Logger.DebugAssert(_colorTexture.IsValid, "Invalid color target texture.");
        }
        else if (swapChainManager.TryGetSwapChain(request.swapChainIndex, out var swapChain))
        {
            _colorTexture = swapChain.GetCurrentBackBuffer();
        }
        else
        {
            throw new InvalidOperationException($"Invalid swap chain index: {request.swapChainIndex}");
        }

        var (desc, error) = resourceDatabase.GetResourceDescription(_colorTexture.AsResource());
        if (error.IsFailure)
        {
            throw new InvalidOperationException($"Failed to get resource description for color target texture. Error: {error}");
        }

        _screenSize = new uint2(desc.TextureDescriptor.Width, desc.TextureDescriptor.Height);
    }

    public void Dispose()
    {
        if (_request.swapChainIndex >= 0)
        {
            _swapChainManager.ReleaseSwapChain(_request.swapChainIndex);
        }
    }
}

public static class RenderPipelineUtility
{
    public static void GetVPMatrices(scoped in RenderRequest request, uint2 screenSize, out float4x4 view, out float4x4 projection)
    {
        var aspectScreen = (float)screenSize.x / screenSize.y;

        view = math.inverse(request.view.localToWorld);

        var vfov = 2.0f * math.atan(request.view.sensorSize.y / (2.0f * request.view.focalLength));
        var hfov = 2.0f * math.atan(request.view.sensorSize.x / (2.0f * request.view.focalLength));
        var aspectSensor = request.view.sensorSize.x / request.view.sensorSize.y;

        float vfovF;
        switch (request.view.gateFit)
        {
            case GateFit.Vertical:
                vfovF = vfov;
                break;

            case GateFit.Horizontal:
                // Adjust VFOV so that the sensor width fits the screen width
                var horizontalAspectBuffer = math.tan(hfov * 0.5f);
                vfovF = 2.0f * math.atan(horizontalAspectBuffer / aspectScreen);
                break;

            case GateFit.Fill:
                if (aspectSensor > aspectScreen)
                {
                    goto case GateFit.Vertical;
                }
                else
                {
                    goto case GateFit.Horizontal;
                }

            case GateFit.Overscan:
                if (aspectSensor > aspectScreen)
                {
                    goto case GateFit.Horizontal;
                }
                else
                {
                    goto case GateFit.Vertical;
                }
            default:
                vfovF = vfov;
                break;
        }

        var m_11 = 1.0f / math.tan(vfovF * 0.5f);
        var m_00 = m_11 / aspectScreen;
        var m_22 = request.view.farClipPlane / (request.view.farClipPlane - request.view.nearClipPlane);
        var m_23 = -(request.view.farClipPlane * request.view.nearClipPlane) / (request.view.farClipPlane - request.view.nearClipPlane);

        projection = new float4x4
        (
            m_00, 0, 0, 0,
            0, m_11, 0, 0,
            0, 0, m_22, m_23,
            0, 0, 1, 0
        );
    }
}
