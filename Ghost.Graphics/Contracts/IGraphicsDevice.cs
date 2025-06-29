using Ghost.Graphics.Data;

namespace Ghost.Graphics.Contracts;

public interface IGraphicsDevice : IDisposable
{
    public static abstract IGraphicsDevice Create();

    public IRenderView CreateRenderView(in SwapChainPresenter swapChainSurface);
    public void OnRender();
}