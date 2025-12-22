using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderPasses;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the renderer interface using RHI abstractions
/// </summary>
internal class D3D12Renderer : IRenderer
{
    private struct FrameResource : IDisposable
    {
        public ICommandBuffer commandBuffer;
        public ulong fenceValue;

        public FrameResource(D3D12GraphicsEngine graphicsEngine, int index)
        {
            commandBuffer = graphicsEngine.CreateCommandBuffer();
            commandBuffer.Name = $"Frame Command Buffer {index}";
            fenceValue = 0;
        }

        public readonly void Dispose()
        {
            commandBuffer?.Dispose();
        }
    }

    private readonly D3D12GraphicsEngine _graphicsEngine;
    private uint _frameIndex;

    private readonly D3D12ResourceDatabase _resourceDatabase;

    private Handle<Texture> _renderTarget;

    private bool _disposed;

    // NOTE: Testing only.
    private readonly MeshRenderPass _pass;

    public Handle<Texture> RenderTarget => _renderTarget;

    // TODO: Add render passes support
    // private ImmutableArray<IRenderPass> _renderPasses;

    public D3D12Renderer(D3D12GraphicsEngine graphicsEngine, D3D12ResourceDatabase resourceDatabase)
    {
        _graphicsEngine = graphicsEngine;
        _resourceDatabase = resourceDatabase;

        _renderTarget = Handle<Texture>.Invalid;


        // NOTE: Testing only.
        _pass = new();
    }

    ~D3D12Renderer()
    {
        Dispose();
    }

    public void SetRenderTarget(Handle<Texture> renderTarget)
    {
        _resourceDatabase.ReleaseResource(_renderTarget.AsResource());
        _renderTarget = renderTarget;
    }

    public Result Render(ICommandBuffer commandBuffer)
    {
        if (!_renderTarget.IsValid)
        {
            return Result.Failure("Render target is not set.");
        }

        commandBuffer.Begin();

        // NOTE: Temperary solution: render directly to the swap chain back buffer if available.
        // HACK: This is hard coded for testing purposes only.

        var error = RenderScene(_renderTarget, commandBuffer);
        if (error != ErrorStatus.None)
        {
            commandBuffer.End();
            return Result.Failure(error);
        }

        return commandBuffer.End();
    }

    // TODO: A proper render graph integration.
    private ErrorStatus RenderScene(Handle<Texture> target, ICommandBuffer cmd)
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
        var ctx = new RenderingContext(_graphicsEngine, cmd, _graphicsEngine.CopyCommandBuffer, null!);
        if (_frameIndex == 0)
        {
            _pass.Initialize(ref ctx);
        }

        var result = _resourceDatabase.GetResourceDescription(target.AsResource());
        if (result.IsFailure)
        {
            return result.Error;
        }

        var texDesc = result.Value.TextureDescription;
        var viewport = new ViewportDesc { Width = texDesc.Width, Height = texDesc.Height, MinDepth = 0, MaxDepth = 1 };
        var scissor = new RectDesc { Right = texDesc.Width, Bottom = texDesc.Height };

        cmd.BeginRenderPass(rtDesc, depthDesc, false);
        cmd.SetViewport(viewport);
        cmd.SetScissorRect(scissor);

        // NOTE: Testing only.
        _pass.Execute(ref ctx);

        cmd.EndRenderPass();

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

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
