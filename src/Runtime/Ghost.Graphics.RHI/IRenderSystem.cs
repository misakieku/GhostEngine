using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.RHI;

public interface IFenceSynchronizer
{
    uint CPUFenceValue
    {
        get;
    }

    uint GPUFenceValue
    {
        get;
    }

    uint FrameIndex
    {
        get;
    }

    uint MaxFrameLatency
    {
        get;
    }

    bool WaitForGPUReady(int timeOut = -1);
    void SignalCPUReady();
    void WaitIdle();
}

public interface IRenderSystem : IFenceSynchronizer, IDisposable
{
    IGraphicsEngine GraphicsEngine
    {
        get;
    }

    bool IsRunning
    {
        get;
    }

    void Start();
    void Stop();
    void RequestSwapChainResize(ISwapChain swapChain, uint2 newSize);
}