using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderPasses;
using Ghost.Graphics.Contracts;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the renderer interface using RHI abstractions
/// </summary>
internal class D3D12Renderer : IRenderer
{
    private readonly D3D12GraphicsEngine _graphicsEngine;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private readonly ICommandBuffer _commandBuffer;

    private uint _frameIndex;
    private bool _disposed;

    // NOTE: Testing only.
    private readonly MeshRenderPass _pass;

    public IRenderTargetStrategy? RenderTargetStrategy
    {
        get; set;
    }

    // TODO: Add render graph support

    public D3D12Renderer(D3D12GraphicsEngine graphicsEngine, D3D12ResourceDatabase resourceDatabase)
    {
        _graphicsEngine = graphicsEngine;
        _resourceDatabase = resourceDatabase;

        _commandBuffer = _graphicsEngine.CreateCommandBuffer(CommandBufferType.Graphics);

        // NOTE: Testing only.
        _pass = new();
    }

    ~D3D12Renderer()
    {
        Dispose();
    }

    public Result Render(ICommandAllocator commandAllocator)
    {
        if (RenderTargetStrategy is null)
        {
            return Result.Failure("Render target strategy is not set.");
        }

        var target = RenderTargetStrategy.GetRenderTarget();
        if (target.IsNotValid)
        {
            return Result.Failure("Render target is invalid.");
        }

        _commandBuffer.Begin(commandAllocator);
        RenderTargetStrategy.BeginRender(_commandBuffer);

        // NOTE: Temperary solution: render directly to the swap chain back buffer if available.
        // HACK: This is hard coded for testing purposes only.

        var error = RenderScene(target);
        if (error != ErrorStatus.None)
        {
            _commandBuffer.End();
            return Result.Failure(error);
        }

        RenderTargetStrategy.EndRender(_commandBuffer);
        var r = _commandBuffer.End();
        if (r.IsFailure)
        {
            return r;
        }

        _graphicsEngine.Device.GraphicsQueue.Submit(_commandBuffer);
        RenderTargetStrategy.Present();

        return Result.Success();
    }

    // TODO: A proper render graph integration.
    private ErrorStatus RenderScene(Handle<Texture> target)
    {
        var clearColor = new Color128 { r = 1.0f, g = 0.0f, b = 1.0f, a = 1.0f };

        Span<PassRenderTargetDesc> rtDesc =
        [
            new PassRenderTargetDesc
            {
                Texture = target,
                ClearColor = clearColor,
            },
        ];

        var depthDesc = new PassDepthStencilDesc
        {
            Texture = Handle<Texture>.Invalid,
            ClearDepth = 1.0f,
            ClearStencil = 0,
        };

        // NOTE: Testing only.
        var ctx = new RenderingContext(_graphicsEngine, _commandBuffer);
        if (_frameIndex == 0)
        {
            _pass.Initialize(ref ctx);
        }

        var result = _resourceDatabase.GetResourceDescription(target.AsResource());
        if (result.IsFailure)
        {
            return result.Error;
        }

        // TODO: Decouple viewport and scissor from the texture size.
        var texDesc = result.Value.TextureDescription;
        var viewport = new ViewportDesc { Width = texDesc.Width, Height = texDesc.Height, MinDepth = 0, MaxDepth = 1 };
        var scissor = new RectDesc { Right = texDesc.Width, Bottom = texDesc.Height };

        _commandBuffer.BeginRenderPass(rtDesc, depthDesc, false);
        _commandBuffer.SetViewport(viewport);
        _commandBuffer.SetScissorRect(scissor);

        // NOTE: Testing only.
        _pass.Execute(ref ctx);

        _commandBuffer.EndRenderPass();
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

        _commandBuffer.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
