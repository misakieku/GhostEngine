using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderPasses;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.RenderGraphModule;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the renderer interface using RHI abstractions
/// </summary>
internal class D3D12Renderer : IRenderer
{
    private readonly D3D12GraphicsEngine _graphicsEngine;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private readonly ICommandBuffer _commandBuffer;
    private readonly RenderGraph _renderGraph;

    private uint _frameIndex;
    private bool _disposed;

    // NOTE: Testing only.
    private readonly MeshRenderPass _pass;

    public IRenderOutput? RenderOutput
    {
        get; set;
    }

    // TODO: Add render graph support

    public D3D12Renderer(D3D12GraphicsEngine graphicsEngine, D3D12ResourceDatabase resourceDatabase)
    {
        _graphicsEngine = graphicsEngine;
        _resourceDatabase = resourceDatabase;

        _commandBuffer = _graphicsEngine.CreateCommandBuffer(CommandBufferType.Graphics);
        _renderGraph = new RenderGraph(_graphicsEngine);

        // NOTE: Testing only.
        _pass = new();
    }

    ~D3D12Renderer()
    {
        Dispose();
    }

    public Result Render(ICommandAllocator commandAllocator)
    {
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

        // NOTE: Temporary solution: render directly to the swap chain back buffer if available.
        // HACK: This is hard coded for testing purposes only.

        var error = RenderScene(target, RenderOutput.Viewport, RenderOutput.Scissor);
        if (error != ErrorStatus.None)
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

    // TODO: A proper render graph integration.
    private ErrorStatus RenderScene(Handle<Texture> target, ViewportDesc viewport, RectDesc rect)
    {
        // NOTE: Testing only.
        var ctx = new RenderingContext(_graphicsEngine, _commandBuffer);
        if (_frameIndex == 0)
        {
            _pass.Initialize(ref ctx);
        }

        //_commandBuffer.BeginRenderPass(rtDesc, depthDesc, false);
        _commandBuffer.SetViewport(viewport);
        _commandBuffer.SetScissorRect(rect);

        _renderGraph.Reset();

        var backBuffer = _renderGraph.ImportTexture(target, "Back Buffer");
        _pass.Build(_renderGraph, backBuffer);

        // Create view state from viewport
        var viewState = new ViewState((uint)viewport.Width, (uint)viewport.Height);

        // Compile with view state
        _renderGraph.Compile(in viewState);
        _renderGraph.Execute(_commandBuffer);

        //_commandBuffer.EndRenderPass();
        _frameIndex++;

        return ErrorStatus.None;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // NOTE: Testing only.
        _pass.Cleanup(_resourceDatabase);
        _renderGraph.Dispose();

        _commandBuffer.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
