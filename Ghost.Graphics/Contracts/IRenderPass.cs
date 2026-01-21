using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Contracts;

public interface IRenderPass
{
    void Initialize(ref readonly RenderingContext ctx);
    void Build(RenderGraph graph, Identifier<RGTexture> backbuffer);
    void Cleanup(IResourceDatabase resourceDatabase);
}
