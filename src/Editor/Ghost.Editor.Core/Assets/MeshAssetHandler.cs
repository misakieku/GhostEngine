using Ghost.Core;
using Ghost.Engine;
using Ghost.Editor.Core.Services;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

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

public sealed class ModelManifestSubAsset
{
    public Guid Guid
    {
        get; set;
    }

    public string Name
    {
        get; set;
    } = string.Empty;

    public string StablePath
    {
        get; set;
    } = string.Empty;

    public int MaterialSlotCount
    {
        get; set;
    }

    public int VertexCount
    {
        get; set;
    }

    public int IndexCount
    {
        get; set;
    }
}

public sealed class ModelManifestMetadata
{
    public string Kind
    {
        get; set;
    } = string.Empty;

    public string Name
    {
        get; set;
    } = string.Empty;

    public string StablePath
    {
        get; set;
    } = string.Empty;
}

internal sealed class ImportedModelAsset : IAsset
{
    public Guid ID
    {
        get;
    }

    public Guid TypeID => typeof(FBXAsset).GUID;

    public IAssetSettings? Settings
    {
        get;
    }

    public ModelManifest Manifest
    {
        get;
    }

    public ImportedModelAsset(Guid id, IAssetSettings? settings, ModelManifest manifest)
    {
        ID = id;
        Settings = settings;
        Manifest = manifest;
    }

    public void Dispose()
    {
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

[CustomAssetHandler(FBXAsset.GUID, [".fbx", ".obj"], 1)]
internal class FBXAssetHandler : ISubAssetImportableAssetHandler, IPackableAssetHandler
{
    public AssetType RuntimeAssetType => AssetType.Mesh;

    public Guid EditorAssetTypeID => typeof(FBXAsset).GUID;

    public bool CanExport => false;

    public IAssetSettings? CreateDefaultSettings()
    {
        return new FbxAssetSettings();
    }

    public async ValueTask<Result<IAsset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        var importedPath = ImportCoordinator.GetImportedAssetPath(id);
        if (!File.Exists(importedPath))
        {
            return Result.Failure<IAsset>("Imported model manifest does not exist.");
        }

        try
        {
            await using var stream = new FileStream(importedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var manifest = await JsonSerializer.DeserializeAsync<ModelManifest>(stream, cancellationToken: token).ConfigureAwait(false);
            return manifest != null
                ? Result.Success<IAsset>(new ImportedModelAsset(id, settings, manifest))
                : Result.Failure<IAsset>("Failed to deserialize model manifest.");
        }
        catch (Exception ex)
        {
            return Result.Failure<IAsset>(ex.Message);
        }
    }

    public ValueTask<Result> SaveAssetAsync(string targetPath, IAsset asset, CancellationToken token = default)
    {
        return ValueTask.FromResult(Result.Failure("Saving model assets is not supported yet."));
    }

    public async ValueTask<Result> ImportAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        return await ImportWithSubAssetsAsync(sourcePath, targetPath, id, settings, token).ConfigureAwait(false);
    }

    public async ValueTask<Result<ImportedSubAsset[]>> ImportWithSubAssetsAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        if (!File.Exists(sourcePath))
        {
            return Result.Failure<ImportedSubAsset[]>("Source file does not exist.");
        }

        var meshSettings = ResolveSettings(sourcePath, settings);
        var root = new MeshNode();
        try
        {
            var parseJob = new MeshParsingJob(root, sourcePath, AllocationHandle.Persistent, meshSettings);
            var context = default(Misaki.HighPerformance.Jobs.JobExecutionContext);
            parseJob.Execute(in context);

            var manifest = new ModelManifest
            {
                AssetId = id,
            };

            var importedSubAssets = new List<ImportedSubAsset>();
            manifest.Root = await WriteNodeAsync(id, sourcePath, root, string.Empty, manifest, importedSubAssets, token).ConfigureAwait(false);

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await using (var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, manifest, cancellationToken: token).ConfigureAwait(false);
            }

            return importedSubAssets.ToArray();
        }
        catch (Exception ex)
        {
            return Result.Failure<ImportedSubAsset[]>($"Failed to import mesh asset: {ex.Message}");
        }
        finally
        {
            root.Dispose();
        }
    }

    public ValueTask<Result> ExportAsync(string assetPath, string targetPath, IAssetExportOptions? options, CancellationToken token = default)
    {
        return ValueTask.FromResult(Result.Failure("Exporting model assets is not supported yet."));
    }

    public ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default)
    {
        return ValueTask.FromResult(Result.Failure("Packing model assets is not supported yet."));
    }

    private static MeshAssetSettings ResolveSettings(string sourcePath, IAssetSettings? settings)
    {
        if (settings is MeshAssetSettings meshSettings)
        {
            return meshSettings;
        }

        return Path.GetExtension(sourcePath).Equals(".obj", StringComparison.OrdinalIgnoreCase)
            ? new ObjAssetSettings()
            : new FbxAssetSettings();
    }

    private static async ValueTask<ModelManifestNode> WriteNodeAsync(
        Guid parentGuid,
        string sourcePath,
        MeshNode node,
        string parentPath,
        ModelManifest manifest,
        List<ImportedSubAsset> importedSubAssets,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var stablePath = string.IsNullOrEmpty(parentPath)
            ? SanitizePathSegment(node.Name)
            : $"{parentPath}/{SanitizePathSegment(node.Name)}";

        var manifestNode = new ModelManifestNode
        {
            Name = node.Name,
            StablePath = stablePath,
            LocalTransform = node.LocalTransform,
        };

        if (node is GeometryMeshNode geometry)
        {
            var meshGuid = CreateDeterministicSubAssetGuid(parentGuid, "Mesh", stablePath);
            var meshPath = ImportCoordinator.GetImportedAssetPath(meshGuid);
            Directory.CreateDirectory(Path.GetDirectoryName(meshPath)!);

            var meshInfo = await WriteMeshContentAsync(meshPath, geometry, token).ConfigureAwait(false);
            manifestNode.MeshGuid = meshGuid;

            manifest.Meshes.Add(new ModelManifestSubAsset
            {
                Guid = meshGuid,
                Name = node.Name,
                StablePath = stablePath,
                MaterialSlotCount = meshInfo.materialSlotCount,
                VertexCount = geometry.Vertices.Count,
                IndexCount = geometry.Indices.Count,
            });

            importedSubAssets.Add(new ImportedSubAsset(
                meshGuid,
                "Mesh",
                node.Name,
                stablePath,
                $"{sourcePath}#Mesh/{stablePath}",
                typeof(FBXAsset).GUID));
        }
        else if (node is LightMeshNode)
        {
            manifest.Metadata.Add(new ModelManifestMetadata
            {
                Kind = "Light",
                Name = node.Name,
                StablePath = stablePath,
            });
        }

        foreach (var child in node.Children)
        {
            manifestNode.Children.Add(await WriteNodeAsync(parentGuid, sourcePath, child, stablePath, manifest, importedSubAssets, token).ConfigureAwait(false));
        }

        return manifestNode;
    }

    private static ValueTask<(int materialSlotCount, int lodLevelCount)> WriteMeshContentAsync(string targetPath, GeometryMeshNode geometry, CancellationToken token)
    {
        unsafe
        {
            var meshletData = new MeshletMeshData();
            try
            {
                MeshProcessor.BuildMeshlets(&meshletData, geometry.Vertices.AsReadOnly(), geometry.Indices.AsReadOnly(), geometry.MaterialParts.AsSpan());
                MeshProcessor.BuildClusterLodHierarchy(&meshletData);

                var bounds = ComputeBounds(geometry.Vertices);
                var header = new MeshContentHeader
                {
                    magic = MeshContentHeader.MAGIC,
                    version = MeshContentHeader.VERSION,
                    vertexCount = (uint)geometry.Vertices.Count,
                    indexCount = (uint)geometry.Indices.Count,
                    materialPartCount = (uint)geometry.MaterialParts.Length,
                    meshletCount = (uint)meshletData.meshlets.Count,
                    meshletGroupCount = (uint)meshletData.groups.Count,
                    meshletHierarchyNodeCount = (uint)meshletData.hierarchyNodes.Count,
                    meshletVertexCount = (uint)meshletData.meshletVertices.Count,
                    meshletTriangleCount = (uint)meshletData.meshletTriangles.Count,
                    materialSlotCount = (uint)meshletData.materialSlotCount,
                    lodLevelCount = (uint)meshletData.lodLevelCount,
                    boundsMin = bounds.Min,
                    boundsMax = bounds.Max,
                };

                using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                WriteStruct(stream, in header);

                header.vertexOffset = (ulong)stream.Position;
                WriteSpan(stream, geometry.Vertices.AsSpan());

                header.indexOffset = (ulong)stream.Position;
                WriteSpan(stream, geometry.Indices.AsSpan());

                header.materialPartOffset = (ulong)stream.Position;
                WriteMaterialParts(stream, geometry.MaterialParts.AsSpan());

                header.meshletOffset = (ulong)stream.Position;
                WriteSpan(stream, meshletData.meshlets.AsSpan());

                header.meshletGroupOffset = (ulong)stream.Position;
                WriteSpan(stream, meshletData.groups.AsSpan());

                header.meshletHierarchyNodeOffset = (ulong)stream.Position;
                WriteSpan(stream, meshletData.hierarchyNodes.AsSpan());

                header.meshletVertexOffset = (ulong)stream.Position;
                WriteSpan(stream, meshletData.meshletVertices.AsSpan());

                header.meshletTriangleOffset = (ulong)stream.Position;
                WriteSpan(stream, meshletData.meshletTriangles.AsSpan());

                stream.Position = 0;
                WriteStruct(stream, in header);
                stream.Flush();

                return ValueTask.FromResult((meshletData.materialSlotCount, meshletData.lodLevelCount));
            }
            finally
            {
                meshletData.Dispose();
            }
        }
    }

    private static AABB ComputeBounds(UnsafeList<Vertex> vertices)
    {
        var min = new float3(float.MaxValue);
        var max = new float3(float.MinValue);
        for (var i = 0; i < vertices.Count; i++)
        {
            var p = vertices[i].position;
            min = math.min(min, p);
            max = math.max(max, p);
        }

        return new AABB(min, max);
    }

    private static Guid CreateDeterministicSubAssetGuid(Guid parentGuid, string kind, string stablePath)
    {
        var bytes = Encoding.UTF8.GetBytes($"{parentGuid:N}:{kind}:{stablePath}");
        Span<byte> hash = stackalloc byte[16];
        var hashValue = XxHash128.HashToUInt128(bytes);
        Unsafe.WriteUnaligned(ref hash[0], hashValue);

        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash);
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Node";
        }

        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] == '/' || chars[i] == '\\' || chars[i] == '#')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    private static void WriteMaterialParts(Stream stream, ReadOnlySpan<MaterialPartInfo> parts)
    {
        if (parts.IsEmpty)
        {
            return;
        }

        Span<MeshContentMaterialPart> buffer = parts.Length <= 64
            ? stackalloc MeshContentMaterialPart[parts.Length]
            : new MeshContentMaterialPart[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            buffer[i] = new MeshContentMaterialPart
            {
                materialIndex = parts[i].materialIndex,
                indexStart = parts[i].indexStart,
                indexCount = parts[i].indexCount,
                vertexStart = parts[i].vertexStart,
                vertexCount = parts[i].vertexCount,
            };
        }

        WriteSpan(stream, buffer);
    }

    private static void WriteStruct<T>(Stream stream, ref readonly T value)
        where T : unmanaged
    {
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in value, 1));
        stream.Write(span);
    }

    private static void WriteSpan<T>(Stream stream, ReadOnlySpan<T> value)
        where T : unmanaged
    {
        if (value.IsEmpty)
        {
            return;
        }

        stream.Write(MemoryMarshal.AsBytes(value));
    }
}
