using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Contracts;

public interface IRenderPass : IDisposable
{
    void Initialize(ICommandBuffer cmd);
    void Execute(ICommandBuffer cmd);
}