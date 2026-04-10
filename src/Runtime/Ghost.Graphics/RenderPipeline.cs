using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics;

public interface IRenderPayload : IDisposable
{
    ReadOnlySpan<RenderRequest> RenderRequests { get; }

    void AddRenderRequest(ref readonly RenderRequest renderRequest);
    void Reset();
}

public interface IRenderPipelineSettings
{
    IRenderPipeline CreatePipeline(RenderSystem renderSystem);
    IRenderPayload CreatePayload(RenderSystem renderSystem, IRenderPipeline renderPipeline);
}

public interface IRenderPipeline : IDisposable
{
    void Render(RenderContext ctx, int frameIndex, IRenderPayload payload);
}

public static class RenderPipelineUtility
{
    public static bool GetVPMatrices(RenderSystem renderSystem, ref readonly RenderRequest request, out float4x4 view, out float4x4 projection, out uint2 screenSize)
    {
        Handle<GPUTexture> rtHandle;
        if (request.swapChainIndex < 0)
        {
            rtHandle = request.colorTarget;
        }
        else if (renderSystem.SwapChainManager.TryGetSwapChain(request.swapChainIndex, out var swapChain))
        {
            rtHandle = swapChain.GetCurrentBackBuffer();
        }
        else
        {
            view = default;
            projection = default;
            screenSize = default;

            return false;
        }

        try
        {
            var rtResult = renderSystem.GraphicsEngine.ResourceDatabase.GetResourceDescription(rtHandle.AsResource());
            if (rtResult.IsFailure)
            {
                view = default;
                projection = default;
                screenSize = default;

                return false;
            }

            screenSize = new uint2(rtResult.Value.TextureDescription.Width, rtResult.Value.TextureDescription.Height);
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

            return true;
        }
        finally
        {
            if (request.swapChainIndex >= 0)
            {
                renderSystem.SwapChainManager.ReleaseSwapChain(request.swapChainIndex);
            }
        }
    }
}