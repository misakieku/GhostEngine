using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;

namespace Ghost.Engine.Components;

public struct MeshInstance : IComponent
{
    public Handle<Mesh> mesh;
    public Identifier<MaterialPalette> materialPalette;
    public RenderingLayerMask renderingLayerMask;
    public ShadowCastingMode shadowCastingMode;
    public bool staticShadowCaster;
}