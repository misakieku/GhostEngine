using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;

namespace Ghost.Engine.Components;

public struct MeshInstance : IComponent
{
    public Handle<Mesh> mesh;
    // NOTE: This will be the first material, we can access other materials by the bindless index of the first material + the local index stored in the meshlet.
    public Handle<Material> materialStart;
    public ShadowCastingMode shadowCastingMode;
    public RenderingLayerMask renderingLayerMask;
    public bool staticShadowCaster;
}
