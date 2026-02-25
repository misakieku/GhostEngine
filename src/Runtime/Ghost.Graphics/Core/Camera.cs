using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Core;

public class Camera
{
    private readonly IRenderer _renderer;

    // History buffers.
    private Handle<Texture> _colorTexture;
    private Handle<Texture> _depthTexture;

    private uint _actualWidth;
    private uint _actualHeight;

    private uint _virtualWidth;
    private uint _virtualHeight;

    public IRenderer Renderer => _renderer;

    /// <summary>
    /// Gets the actual width of the camera's render target in pixels. If upscaler is used, this is the width before upscaling.
    /// </summary>
    public uint ActualWidth => _actualWidth;

    /// <summary>
    /// Gets the actual height of the camera's render target in pixels. If upscaler is used, this is the height before upscaling.
    /// </summary>
    public uint ActualHeight => _actualHeight;

    /// <summary>
    /// Gets the virtual width of the camera's render target in pixels. If upscaler is used, this is the width after upscaling.
    /// </summary>
    public uint VirtualWidth => _virtualWidth;

    /// <summary>
    /// Gets the virtual height of the camera's render target in pixels. If upscaler is used, this is the height after upscaling.
    /// </summary>
    public uint VirtualHeight => _virtualHeight;

    public RenderGraph? RenderGraph
    {
        get; set;
    }

    public Camera(IGraphicsEngine graphicsEngine)
    {
        _renderer = graphicsEngine.CreateRenderer();
    }

    public Camera(IGraphicsEngine graphicsEngine, RenderGraph renderGraph)
    {
        _renderer = graphicsEngine.CreateRenderer();
        RenderGraph = renderGraph;

        _renderer.RenderFunc = DefaultRenderFunc;
    }

    private Error DefaultRenderFunc(RenderContext context)
    {
        if (RenderGraph == null)
        {
            return Error.None;
        }

        RenderGraph.Reset();

        var view = new ViewState(_virtualWidth, _virtualHeight, _actualWidth, _actualHeight);
        var e = RenderGraph.Compile(in view);
        if (e != Error.None)
        {
            return e;
        }

        e = RenderGraph.Execute(context.CommandBuffer);
        if (e != Error.None)
        {
            return e;
        }

        return Error.None;
    }
}