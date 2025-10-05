using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Contracts;

public interface IRenderPass
{
    public void Initialize(ICommandBuffer cmd);
    public void Execute(ICommandBuffer cmd);
    public void Cleanup(IResourceDatabase resourceDatabase);
}