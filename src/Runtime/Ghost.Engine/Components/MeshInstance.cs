using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.Services;

namespace Ghost.Engine.Components;

public struct MeshInstance : IComponentData
{
    public Handle<Mesh> mesh;
    public Identifier<MaterialPalette> materialPalette;
    public RenderingLayerMask renderingLayerMask;
    public ShadowCastingMode shadowCastingMode;
    public bool staticShadowCaster;
}
