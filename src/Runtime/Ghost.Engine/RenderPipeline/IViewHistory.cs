using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

public interface IViewHistory : IDisposable
{
    void OnResize(uint2 newSize);
    void Reset();
}
