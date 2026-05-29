using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Editor.Core.Services;
using Ghost.Engine;
using Ghost.Engine.Streaming;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
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

    public Guid TypeID => typeof(MeshAsset).GUID;

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

[Guid(GUID)]
public abstract class MeshAsset : IAsset
{
    public const string GUID = "B99CA68E-EE7A-4822-BF1C-AA0A5120C36A";

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

[CustomAssetHandler(AssetTypeId = MeshAsset.GUID, RuntimeAssetType = AssetType.Mesh, Extensions = new[] { ".fbx", ".obj" })]
internal class MeshAssetHandler : IImportableAssetHandler, IPackableAssetHandler
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public IAssetSettings? CreateDefaultSettings(string ext)
    {
        if (string.Equals(ext, ".obj", StringComparison.OrdinalIgnoreCase))
        {
            return new ObjAssetSettings();
        }
        else if (string.Equals(ext, ".fbx", StringComparison.OrdinalIgnoreCase))
        {
            return new FbxAssetSettings();
        }

        return null;
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
            var manifest = await JsonSerializer.DeserializeAsync<ModelManifest>(stream, s_jsonOptions, token).ConfigureAwait(false);
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

    public async ValueTask<Result<ImportedSubAsset[]>> ImportAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        if (!File.Exists(sourcePath))
        {
            return Result.Failure<ImportedSubAsset[]>("Source file does not exist.");
        }

        try
        {
            var meshSettings = ResolveSettings(sourcePath, settings);

            using var root = new MeshNode();
            var result = await MeshProcessor.ParseMeshAsync(root, sourcePath, AllocationHandle.TLSF, meshSettings, token).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            var manifest = new ModelManifest
            {
                AssetId = id,
            };

            var importedSubAssets = new List<ImportedSubAsset>();
            manifest.Root = await WriteNodeAsync(id, sourcePath, root, string.Empty, manifest, importedSubAssets, token).ConfigureAwait(false);

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            await using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, manifest, s_jsonOptions, token).ConfigureAwait(false);

            return importedSubAssets.ToArray();
        }
        catch (Exception ex)
        {
            return Result.Failure<ImportedSubAsset[]>($"Failed to import mesh asset: {ex.Message}");
        }
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

    private async ValueTask<ModelManifestNode> WriteNodeAsync(
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

            var (materialSlotCount, lodLevelCount) = await WriteMeshContentAsync(meshPath, geometry, token).ConfigureAwait(false);
            manifestNode.MeshGuid = meshGuid;

            manifest.Meshes.Add(new ModelManifestSubAsset
            {
                Guid = meshGuid,
                Name = node.Name,
                StablePath = stablePath,
                MaterialSlotCount = materialSlotCount,
                VertexCount = geometry.Vertices.Count,
                IndexCount = geometry.Indices.Count,
            });

            importedSubAssets.Add(new ImportedSubAsset(
                meshGuid,
                "Mesh",
                node.Name,
                stablePath,
                $"{sourcePath}#Mesh/{stablePath}",
                typeof(MeshAsset).GUID));
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

    private async ValueTask<(int materialSlotCount, int lodLevelCount)> WriteMeshContentAsync(string targetPath, GeometryMeshNode geometry, CancellationToken token)
    {
        using var meshletData = await MeshProcessor.BuildMeshletsAsync(geometry.Vertices, geometry.Indices, geometry.MaterialParts, token).ConfigureAwait(false);
        await MeshProcessor.BuildClusterLodHierarchyAsync(meshletData.Share(), token).ConfigureAwait(false);

        var bounds = ComputeBounds(geometry.Vertices);
        var header = new MeshContentHeader
        {
            magic = MeshContentHeader.MAGIC,
            version = MeshContentHeader.VERSION,
            vertexCount = geometry.Vertices.Count,
            indexCount = geometry.Indices.Count,
            materialPartCount = geometry.MaterialParts.Length,
            meshletCount = meshletData.GetRef().meshlets.Count,
            meshletGroupCount = meshletData.GetRef().groups.Count,
            meshletHierarchyNodeCount = meshletData.GetRef().hierarchyNodes.Count,
            meshletVertexCount = meshletData.GetRef().meshletVertices.Count,
            meshletTriangleCount = meshletData.GetRef().meshletTriangles.Count,
            materialSlotCount = meshletData.GetRef().materialSlotCount,
            lodLevelCount = meshletData.GetRef().lodLevelCount,
            boundsMin = bounds.Min,
            boundsMax = bounds.Max,
        };

        using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(header);

        header.vertexOffset = stream.Position;
        await stream.WriteAsync<Vertex, UnsafeList<Vertex>>(geometry.Vertices, token);

        header.indexOffset = stream.Position;
        await stream.WriteAsync<uint, UnsafeList<uint>>(geometry.Indices, token);

        header.materialPartOffset = stream.Position;
        WriteMaterialParts(stream, geometry.MaterialParts.AsSpan());

        header.meshletOffset = stream.Position;
        await stream.WriteAsync<Meshlet, UnsafeList<Meshlet>>(meshletData.GetRef().meshlets, token);

        header.meshletGroupOffset = stream.Position;
        await stream.WriteAsync<MeshletGroup, UnsafeList<MeshletGroup>>(meshletData.GetRef().groups, token);

        header.meshletHierarchyNodeOffset = stream.Position;
        await stream.WriteAsync<MeshletHierarchyNode, UnsafeList<MeshletHierarchyNode>>(meshletData.GetRef().hierarchyNodes, token);

        header.meshletVertexOffset = stream.Position;
        await stream.WriteAsync<uint, UnsafeList<uint>>(meshletData.GetRef().meshletVertices, token);

        header.meshletTriangleOffset = stream.Position;
        await stream.WriteAsync<uint, UnsafeList<uint>>(meshletData.GetRef().meshletTriangles, token);

        stream.Position = 0;
        stream.Write(header);
        stream.Flush();

        return (meshletData.GetRef().materialSlotCount, meshletData.GetRef().lodLevelCount);
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
        for (var i = 0; i < value.Length; i++)
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

        var buffer = parts.Length <= 64
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

        stream.Write(buffer);
    }
}
