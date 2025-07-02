using Ghost.Graphics.Data;

namespace Ghost.Graphics.Contracts;

internal interface IGraphicsDevice : IDisposable
{
    public static abstract GraphicsAPI TargetAPI
    {
        get;
    }

    public ReadOnlySpan<IRenderer> Renderers
    {
        get;
    }

    public IRenderer CreateRenderer(in SwapChainPresenter swapChainSurface);
    public void RemoveRenderer(IRenderer renderer);
    public void InitializePendingRenderers();
}