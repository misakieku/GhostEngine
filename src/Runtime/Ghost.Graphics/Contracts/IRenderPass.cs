using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;

namespace Ghost.Graphics.Core.Contracts;

public interface IRenderPass
{
    void Initialize(ref readonly RenderingContext ctx);
    void Build(RenderGraph graph, Identifier<RGTexture> backbuffer);
    void Cleanup(IResourceManager resourceManager);
}
