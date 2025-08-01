using Ghost.Graphics.D3D12;

namespace Ghost.Graphics.Contracts;

public interface IRenderPass : IDisposable
{
    void Initialize(CommandList cmd);
    void Execute(CommandList cmd);
}