namespace Ghost.Graphics.Data;

public struct BatchMaterialID : IEquatable<BatchMaterialID>
{
    public uint value;

    public static BatchMaterialID Null => new() { value = 0 };

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is BatchMaterialID id && Equals(id);
    }

    public readonly bool Equals(BatchMaterialID other)
    {
        return value == other.value;
    }

    public readonly int CompareTo(BatchMaterialID other)
    {
        return value.CompareTo(other.value);
    }

    public static bool operator ==(BatchMaterialID a, BatchMaterialID b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(BatchMaterialID a, BatchMaterialID b)
    {
        return !a.Equals(b);
    }
}

internal class GraphicsResourceManager
{
    private readonly Dictionary<BatchMaterialID, Material> _materials = new();
    private readonly Dictionary<BatchMeshID, MeshHandle> _meshes = new();

    public Material GetMaterial(BatchMaterialID id)
    {
        return _materials.TryGetValue(id, out var material) ? material : Material.Null;
    }

    public MeshHandle GetMesh(BatchMeshID id)
    {
        return _meshes.TryGetValue(id, out var mesh) ? mesh : MeshHandle.Invalid;
    }
}