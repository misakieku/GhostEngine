using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Core;

internal class SwapChainRenderOutput : IRenderOutput
{
    private readonly ISwapChain _swapChain;

    public ViewportDesc Viewport
    {
        get; set;
    }

    public ScissorRectDesc Scissor
    {
        get; set;
    }

    public SwapChainRenderOutput(ISwapChain swapChain)
    {
        _swapChain = swapChain;

        Viewport = new ViewportDesc { Width = swapChain.Width, Height = swapChain.Height, MinDepth = 0, MaxDepth = 1 };
        Scissor = new ScissorRectDesc { Right = swapChain.Width, Bottom = swapChain.Height };
    }

    public Handle<Texture> GetRenderTarget()
    {
        return _swapChain.GetCurrentBackBuffer();
    }

    public void BeginRender(ICommandBuffer cmd)
    {
        var barrierDesc = BarrierDesc.Texture(_swapChain.GetCurrentBackBuffer().AsResource(),
            BarrierSync.None, BarrierSync.RenderTarget,
            BarrierAccess.NoAccess, BarrierAccess.RenderTarget,
            BarrierLayout.Present, BarrierLayout.RenderTarget);

        cmd.Barrier(barrierDesc);
    }

    public void EndRender(ICommandBuffer cmd)
    {
        var barrierDesc = BarrierDesc.Texture(_swapChain.GetCurrentBackBuffer().AsResource(),
            BarrierSync.RenderTarget, BarrierSync.None,
            BarrierAccess.RenderTarget, BarrierAccess.NoAccess,
            BarrierLayout.RenderTarget, BarrierLayout.Present);

        cmd.Barrier(barrierDesc);
    }

    public void Present()
    {
        _swapChain.Present();
    }
}

internal class TextureRenderOutput : IRenderOutput
{
    private readonly Handle<Texture> _texture;

    public ViewportDesc Viewport
    {
        get; set;
    }

    public ScissorRectDesc Scissor
    {
        get; set;
    }

    public TextureRenderOutput(Handle<Texture> texture)
    {
        _texture = texture;
    }

    public Handle<Texture> GetRenderTarget()
    {
        return _texture;
    }

    public void BeginRender(ICommandBuffer cmd)
    {
    }

    public void EndRender(ICommandBuffer cmd)
    {
    }

    public void Present()
    {
    }
}
