using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Contracts;

public interface IRenderPass
{
    public void Initialize(ref readonly RenderingContext ctx);
    public void Execute(ref readonly RenderingContext ctx);
    public void Cleanup(IResourceDatabase resourceDatabase);
}
