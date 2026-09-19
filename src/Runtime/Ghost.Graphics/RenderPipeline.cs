using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics;

public interface IRenderPayload : IDisposable
{
    /// <summary>
    /// The list of render requests to be processed by the render pipeline.
    /// </summary>
    ReadOnlySpan<RenderRequest> RenderRequests { get; }

    /// <summary>
    /// Adds a render request to the stateless payload, which will be processed by the render pipeline during the rendering phase.
    /// </summary>
    /// <remarks>
    /// This method is not thread-safe and should be called from the main thread.
    /// </remarks>
    /// <param name="renderRequest">The render request to be added.</param>
    void AddRenderRequest(scoped in RenderRequest renderRequest);

    /// <summary>
    /// Resets the payload, clearing all render requests and preparing it for the next frame.
    /// </summary>
    void Reset();

    /// <summary>
    /// Begins recording for the frame on the simulation thread.
    /// </summary>
    void BeginRecord() { }

    /// <summary>
    /// Finalizes recording of the payload for the current frame before handing off to the render thread.
    /// </summary>
    void EndRecord() { }
}

public interface IRenderPipeline : IDisposable
{
    /// <summary>
    /// Creates a new per-frame payload instance for this render pipeline.
    /// </summary>
    IRenderPayload CreatePayload();

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
    Result ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload,
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

public static unsafe class RenderPipelineUtility
{
    public static void GetVPMatrices(scoped in RenderRequest request, uint2 screenSize, out float4x4 view, out float4x4 projection, bool reversedZ = false)
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

        float m_22;
        float m_23;

        if (reversedZ)
        {
            // Reversed-Z: Near -> 1.0, Far -> 0.0
            m_22 = request.view.nearClipPlane / (request.view.nearClipPlane - request.view.farClipPlane);
            m_23 = (request.view.farClipPlane * request.view.nearClipPlane) / (request.view.farClipPlane - request.view.nearClipPlane);
        }
        else
        {
            // Standard Z: Near -> 0.0, Far -> 1.0
            m_22 = request.view.farClipPlane / (request.view.farClipPlane - request.view.nearClipPlane);
            m_23 = -(request.view.farClipPlane * request.view.nearClipPlane) / (request.view.farClipPlane - request.view.nearClipPlane);
        }

        projection = new float4x4
        (
            m_00, 0, 0, 0,
            0, m_11, 0, 0,
            0, 0, m_22, m_23,
            0, 0, 1, 0
        );
    }

    public static void GetVPMatricesReversedZ(scoped in RenderRequest request, uint2 screenSize, out float4x4 view, out float4x4 projection)
    {
        GetVPMatrices(in request, screenSize, out view, out projection, reversedZ: true);
    }

    public static Handle<GPUBuffer> CreateFrameBuffer(RenderContext ctx, uint sceneBuffer)
    {
        var frameData = new FrameData
        {
            sceneBuffer = sceneBuffer,
            userBuffer = 0,
            paletteOffsetBuffer = ctx.ResourceManager.PaletteOffsetBufferBindlessIndex,
            materialIndexBuffer = ctx.ResourceManager.MaterialIndexBufferBindlessIndex,
        };

        var alignedSize = ((uint)sizeof(FrameData) + 255u) & ~255u;
        var frameDesc = new BufferDesc
        {
            Size = alignedSize,
            Stride = alignedSize,
            Usage = BufferUsage.Constant | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload,
        };

        var frameGpuBuffer = ctx.ResourceManager.CreateTransientBuffer(in frameDesc, "FrameDataBuffer");
        var pFrameData = (FrameData*)ctx.ResourceDatabase.MapResource(frameGpuBuffer.AsResource(), 0, null);
        *pFrameData = frameData;
        ctx.ResourceDatabase.UnmapResource(frameGpuBuffer.AsResource(), 0, null);
        return frameGpuBuffer;
    }

    public static Handle<GPUBuffer> CreateViewDataBuffer(RenderContext ctx, RenderRequest request, RenderViewData renderView, float4x4 viewMatrix, float4x4 projMatrix, float4x4 viewProjMatrix, Frustum frustum, ref float4x4 prevVP)
    {
        var viewData = new ViewData
        {
            viewMatrix = viewMatrix,
            projectionMatrix = projMatrix,
            viewProjectionMatrix = viewProjMatrix,
            preVPMatrix = prevVP,
            cameraPosition = request.view.localToWorld.c3.xyz,
            nearClip = request.view.nearClipPlane,
            cameraDirection = request.view.localToWorld.c2.xyz,
            farClip = request.view.farClipPlane,
            screenSize = new float4(renderView.ScreenSize.x, renderView.ScreenSize.y, 1.0f / renderView.ScreenSize.x, 1.0f / renderView.ScreenSize.y),
            frustum = frustum
        };

        prevVP = viewProjMatrix;

        var alignedSize = ((uint)sizeof(ViewData) + 255u) & ~255u;
        var viewDesc = new BufferDesc
        {
            Size = alignedSize,
            Stride = alignedSize,
            Usage = BufferUsage.Constant | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload,
        };
        var viewGpuBuffer = ctx.ResourceManager.CreateTransientBuffer(in viewDesc, "ViewDataBuffer");
        var pViewData = (ViewData*)ctx.ResourceDatabase.MapResource(viewGpuBuffer.AsResource(), 0, null);
        *pViewData = viewData;
        ctx.ResourceDatabase.UnmapResource(viewGpuBuffer.AsResource(), 0, null);
        return viewGpuBuffer;
    }
}
