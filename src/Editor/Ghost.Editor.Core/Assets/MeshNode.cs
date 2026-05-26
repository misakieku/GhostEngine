using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Assets;

public class MeshNode : IDisposable
{
    public string Name
    {
        get; set;
    } = string.Empty;

    public float4x4 LocalTransform
    {
        get; set;
    }

    public MeshNode? Parent
    {
        get; set;
    }

    public IReadOnlyCollection<MeshNode> Children
    {
        get; set;
    } = Array.Empty<MeshNode>();

    ~MeshNode()
    {
        Dispose(false);
    }

    public MeshNode Clone()
    {
        return (MeshNode)MemberwiseClone();
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        foreach (var child in Children)
        {
            child.Dispose();
        }

        Parent = null;
        Children = Array.Empty<MeshNode>();

        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Describes one material partition within a unified vertex/index buffer.
/// </summary>
public struct MaterialPartInfo
{
    /// <summary> The material slot index (from ufbx face_material). </summary>
    public int materialIndex;
    /// <summary> Byte offset into the unified index buffer. </summary>
    public int indexStart;
    /// <summary> Number of indices belonging to this part. </summary>
    public int indexCount;
    /// <summary> Byte offset into the unified vertex buffer. </summary>
    public int vertexStart;
    /// <summary> Number of unique vertices belonging to this part. </summary>
    public int vertexCount;
}

public class GeometryMeshNode : MeshNode
{
    private UnsafeList<Vertex> _vertices;
    private UnsafeList<uint> _indices;
    private UnsafeArray<MaterialPartInfo> _materialParts;

    public UnsafeList<Vertex> Vertices
    {
        get => _vertices;
        set
        {
            _vertices.Dispose();
            _vertices = value;
        }
    }

    public UnsafeList<uint> Indices
    {
        get => _indices;
        set
        {
            _indices.Dispose();
            _indices = value;
        }
    }

    public UnsafeArray<MaterialPartInfo> MaterialParts
    {
        get => _materialParts;
        set
        {
            _materialParts.Dispose();
            _materialParts = value;
        }
    }

    protected override void Dispose(bool disposing)
    {
        _vertices.Dispose();
        _indices.Dispose();
        _materialParts.Dispose();
    }
}

public class LightMeshNode : MeshNode
{
    public float3 Color
    {
        get; set;
    }

    public float Intensity
    {
        get; set;
    }
}

public sealed class ModelManifest
{
    public Guid AssetId
    {
        get; set;
    }

    public ModelManifestNode Root
    {
        get; set;
    } = new ModelManifestNode();

    public List<ModelManifestSubAsset> Meshes
    {
        get; set;
    } = new List<ModelManifestSubAsset>();

    public List<ModelManifestMetadata> Metadata
    {
        get; set;
    } = new List<ModelManifestMetadata>();
}

public sealed class ModelManifestNode
{
    public string Name
    {
        get; set;
    } = string.Empty;

    public string StablePath
    {
        get; set;
    } = string.Empty;

    public float4x4 LocalTransform
    {
        get; set;
    }

    public Guid MeshGuid
    {
        get; set;
    }

    public List<ModelManifestNode> Children
    {
        get; set;
    } = new List<ModelManifestNode>();
}
