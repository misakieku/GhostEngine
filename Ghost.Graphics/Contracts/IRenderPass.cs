using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Contracts;

public interface IRenderPass
{
    void Initialize(ref readonly RenderingContext ctx);
    void Execute(ref readonly RenderingContext ctx);
    void Cleanup(IResourceDatabase resourceDatabase);
}
