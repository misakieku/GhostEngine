using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Engine.Components;

public struct MeshPalette : ISharedComponent, IEquatable<MeshPalette>
{
    public UnsafeArray<Handle<Mesh>> meshes;
    public UnsafeArray<Handle<Material>> materials;

    public bool Equals(MeshPalette other)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object? obj)
    {
        return obj is MeshPalette palette && Equals(palette);
    }

    public static bool operator ==(MeshPalette left, MeshPalette right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MeshPalette left, MeshPalette right)
    {
        return !(left == right);
    }
}

public struct MeshInstance : IComponent
{
    public int meshIndex;
    public int materialIndex;
    public ShadowCastingMode shadowCastingMode;
    public RenderingLayerMask renderingLayerMask;
    public byte subMeshIndex;
    public bool staticShadowCaster;
}
