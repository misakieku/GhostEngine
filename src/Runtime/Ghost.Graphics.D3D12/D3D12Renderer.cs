using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the renderer interface using RHI abstractions
/// </summary>
internal class D3D12Renderer : IRenderer
{
    private readonly D3D12GraphicsEngine _graphicsEngine;
    private readonly ICommandBuffer _commandBuffer;

    private bool _disposed;

    public IRenderOutput? RenderOutput
    {
        get; set;
    }

    public Func<RenderContext, Error>? RenderFunc
    {
        get; set;
    }

    public D3D12Renderer(D3D12GraphicsEngine graphicsEngine)
    {
        _graphicsEngine = graphicsEngine;
        _commandBuffer = _graphicsEngine.CreateCommandBuffer(CommandBufferType.Graphics);
    }

    ~D3D12Renderer()
    {
        Dispose();
    }

    public Result Render(ICommandAllocator commandAllocator)
    {
        if (RenderFunc is null)
        {
            return Result.Success(); // No render function set, skip rendering.
        }

        if (RenderOutput is null)
        {
            return Result.Failure("Render target strategy is not set.");
        }

        var target = RenderOutput.GetRenderTarget();
        if (target.IsInvalid)
        {
            return Result.Failure("Render target is invalid.");
        }

        _commandBuffer.Begin(commandAllocator);
        RenderOutput.BeginRender(_commandBuffer);

        var ctx = new RenderContext();
        var error = RenderFunc.Invoke(ctx);
        if (error != Error.None)
        {
            _commandBuffer.End();
            return Result.Failure(error);
        }

        RenderOutput.EndRender(_commandBuffer);
        var r = _commandBuffer.End();
        if (r.IsFailure)
        {
            return r;
        }

        _graphicsEngine.Device.GraphicsQueue.Submit(_commandBuffer);
        RenderOutput.Present();

        return Result.Success();
    }

    //// TODO: A proper render graph integration.
    //private Error RenderScene(Handle<Texture> target, ViewportDesc viewport, RectDesc rect)
    //{
    //    // NOTE: Testing only.
    //    var ctx = new RenderingContext(_graphicsEngine, _commandBuffer);
    //    if (_frameIndex == 0)
    //    {
    //        _pass.Initialize(ref ctx);
    //    }

    //    //_commandBuffer.BeginRenderPass(rtDesc, depthDesc, false);
    //    _commandBuffer.SetViewport(viewport);
    //    _commandBuffer.SetScissorRect(rect);

    //    _renderGraph.Reset();

    //    var backBuffer = _renderGraph.ImportTexture(target, "Back Buffer");
    //    _pass.Build(_renderGraph, backBuffer);

    //    // Create view state from viewport
    //    var viewState = new ViewState((uint)viewport.Width, (uint)viewport.Height);

    //    // Compile with view state
    //    _renderGraph.Compile(in viewState);
    //    _renderGraph.Execute(_commandBuffer);

    //    //_commandBuffer.EndRenderPass();
    //    _frameIndex++;

    //    return Error.None;
    //}

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _commandBuffer.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
