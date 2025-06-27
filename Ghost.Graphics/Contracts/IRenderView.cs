namespace Ghost.Graphics.Contracts;

internal interface IRenderView : IDisposable
{
    public void Resize(uint width, uint height);
    public void Render();

    public void WaitNextFrame();
    public void WaitIdle();
    public void Flush();
}