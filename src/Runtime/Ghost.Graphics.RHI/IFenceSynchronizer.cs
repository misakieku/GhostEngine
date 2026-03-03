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
