using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.Services;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Components;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MeshInstance : IComponentData
{
    public Handle<Mesh> mesh;
    public Identifier<MaterialPalette> materialPalette;
    public RenderingLayerMask renderingLayerMask;
    public ShadowCastingMode shadowCastingMode;
    public bool staticShadowCaster;
}
