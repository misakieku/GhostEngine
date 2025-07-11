using Ghost.Graphics.D3D12;

namespace Ghost.Graphics.Contracts;

internal interface IRenderPass : IDisposable
{
    void Initialize(CommandList cmd);
    void Execute(CommandList cmd);
}