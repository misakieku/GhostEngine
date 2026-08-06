using Ghost.Core;

namespace Ghost.Graphics.RenderGraphModule;

internal interface IRenderGraphValidationResourceProvider
{
    RGResourceType GetResourceType(Identifier<RGResource> resource);
    string GetResourceName(Identifier<RGResource> resource);
}
