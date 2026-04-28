using Ghost.Core;
using Ghost.Engine;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

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

public abstract class MeshAsset : IAsset
{
    private MeshNode _root;

    public Guid ID
    {
        get;
    }

    public IAssetSettings Settings
    {
        get;
    }

    public Guid TypeID => typeof(MeshAsset).GUID;

    public MeshNode Root
    {
        get => _root;
        set
        {
            _root?.Dispose();
            _root = value;
        }
    }

    internal MeshAsset(MeshNode root, Guid id, MeshAssetSettings settings)
    {
        _root = root;

        ID = id;
        Settings = settings;
    }

    public void Dispose()
    {
        _root?.Dispose();
    }
}

[Guid(GUID)]
public partial class FBXAsset : MeshAsset
{
    public const string GUID = "B99CA68E-EE7A-4822-BF1C-AA0A5120C36A";

    internal FBXAsset(MeshNode root, Guid id, FbxAssetSettings settings)
        : base(root, id, settings)
    {
    }
}

public enum CoordinateAxis
{
    PositiveX,
    PositiveY,
    PositiveZ,
    NegativeX,
    NegativeY,
    NegativeZ
}

public enum VertexDataSource
{
    Imported,
    Computed,
    ComputedIfMissing
}

public class MeshAssetSettings : IAssetSettings
{
    public VertexDataSource NormalDataSource
    {
        get; set;
    } = VertexDataSource.ComputedIfMissing;

    public VertexDataSource TangentDataSource
    {
        get; set;
    } = VertexDataSource.ComputedIfMissing;
}

internal class ObjAssetSettings : MeshAssetSettings
{
    public CoordinateAxis ObjectUpAxis
    {
        get; set;
    } = CoordinateAxis.PositiveY;

    public CoordinateAxis ObjectForwardAxis
    {
        get; set;
    } = CoordinateAxis.NegativeZ;

    public CoordinateAxis ObjectRightAxis
    {
        get; set;
    } = CoordinateAxis.PositiveX;

    public float UnitMeterScale
    {
        get; set;
    } = 1.0f;
}

internal class FbxAssetSettings : MeshAssetSettings
{
}

internal class FBXAssetHandler : IImportableAssetHandler, IPackableAssetHandler
{
    public AssetType RuntimeAssetType => AssetType.Mesh;

    public Guid EditorAssetTypeID => typeof(FBXAsset).GUID;

    public bool CanExport => false;

    public IAssetSettings? CreateDefaultSettings()
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result<IAsset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result> SaveAssetAsync(string targetPath, IAsset asset, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result> ImportAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result> ExportAsync(string assetPath, string targetPath, IAssetExportOptions? options, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}