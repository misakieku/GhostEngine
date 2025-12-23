using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Core;

internal class SwapChainTargetStrategy : IRenderTargetStrategy
{
    private readonly ISwapChain _swapChain;

    public ViewportDesc Viewport
    {
        get; set;
    }

    public RectDesc Scissor
    {
        get; set;
    }

    public SwapChainTargetStrategy(ISwapChain swapChain)
    {
        _swapChain = swapChain;

        Viewport = new ViewportDesc { Width = swapChain.Width, Height = swapChain.Height, MinDepth = 0, MaxDepth = 1 };
        Scissor = new RectDesc { Right = swapChain.Width, Bottom = swapChain.Height };
    }

    public Handle<Texture> GetRenderTarget()
    {
        return _swapChain.GetCurrentBackBuffer();
    }

    public void BeginRender(ICommandBuffer cmd)
    {
        cmd.ResourceBarrier(GetRenderTarget().AsResource(), ResourceState.Present, ResourceState.RenderTarget);
    }

    public void EndRender(ICommandBuffer cmd)
    {
        cmd.ResourceBarrier(GetRenderTarget().AsResource(), ResourceState.RenderTarget, ResourceState.Present);
    }

    public void Present()
    {
        _swapChain.Present();
    }
}

internal class TextureTargetStrategy : IRenderTargetStrategy
{
    private readonly Handle<Texture> _texture;

    public ViewportDesc Viewport
    {
        get; set;
    }

    public RectDesc Scissor
    {
        get; set;
    }

    public TextureTargetStrategy(Handle<Texture> texture)
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
