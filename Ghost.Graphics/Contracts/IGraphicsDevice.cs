using Ghost.Graphics.Data;

namespace Ghost.Graphics.Contracts;

internal interface IGraphicsDevice : IDisposable
{
    public static abstract IGraphicsDevice Create();

    public IRenderView CreateRenderView(in SwapChainSurface swapChainSurface);
    public void OnRender();
}