using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;

namespace Ghost.Engine.Components;

public struct MeshInstance : IComponent
{
    public Handle<Mesh> mesh;
    public int materialPaletteIndex;
    public ShadowCastingMode shadowCastingMode;
    public RenderingLayerMask renderingLayerMask;
    public bool staticShadowCaster;
}
