using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Core.Bakers;

[AssetBaker(Extensions = [".fbx", ".obj"], Type = AssetType.Mesh, SettingsType = typeof(MeshBakeSettings))]
public unsafe partial class MeshBaker : IAssetBaker
{
    public Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        if (settings is not MeshBakeSettings meshSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected MeshBakeSettings.", nameof(settings));
        }

        var parseResult = MeshProcessor.ParseModel(src, meshSettings, AllocationHandle.Persistent);
        if (parseResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to parse model '{src}': {parseResult.Message}");
        }

        var meshes = parseResult.Value;

        try
        {
            for (var i = 0; i < meshes.Count; i++)
            {
                var mesh = meshes[i];
                cancellationToken.ThrowIfCancellationRequested();

                using var meshletDataPtr = MeshProcessor.BuildMeshlets(
                    mesh.Vertices.AsReadOnly(),
                    mesh.Indices.AsReadOnly(),
                    mesh.MaterialParts.AsReadOnly(),
                    meshSettings,
                    AllocationHandle.Persistent);

                var pMeshletData = meshletDataPtr.Get();
                MeshProcessor.BuildClusterLodHierarchy(pMeshletData, AllocationHandle.Persistent);

                if (i == 0)
                {
                    WriteMeshContent(dst, mesh, pMeshletData);
                }
                else
                {
                    var subPath = string.IsNullOrWhiteSpace(mesh.Name) ? $"Mesh_{i}" : mesh.Name;
                    using var subStream = ctx.AddSubAsset(subPath, AssetType.Mesh);
                    WriteMeshContent(subStream, mesh, pMeshletData);
                }
            }

            return Task.CompletedTask;
        }
        finally
        {
            for (var i = 0; i < meshes.Count; i++)
            {
                meshes[i].Dispose();
            }
        }
    }

    private static void WriteMeshContent(Stream stream, ParsedMesh mesh, MeshletMeshData* meshletData)
    {
        var assetStartOffset = stream.Position;

        var header = new MeshContentHeader
        {
            magic = MeshContentHeader.MAGIC,
            version = MeshContentHeader.VERSION,
            vertexCount = mesh.Vertices.Count,
            indexCount = mesh.Indices.Count,
            materialPartCount = mesh.MaterialParts.Length,
            meshletCount = meshletData->meshlets.Count,
            meshletGroupCount = meshletData->groups.Count,
            meshletHierarchyNodeCount = meshletData->hierarchyNodes.Count,
            meshletVertexCount = meshletData->meshletVertices.Count,
            meshletTriangleCount = meshletData->meshletTriangles.Count,
            materialSlotCount = meshletData->materialSlotCount,
            lodLevelCount = meshletData->lodLevelCount,
            boundsMin = mesh.BoundingBox.Min,
            boundsMax = mesh.BoundingBox.Max,
        };

        // Write header placeholder
        stream.Write(header);

        header.vertexOffset = stream.Position - assetStartOffset;
        stream.Write(mesh.Vertices.AsSpan());

        header.indexOffset = stream.Position - assetStartOffset;
        stream.Write(mesh.Indices.AsSpan());

        header.materialPartOffset = stream.Position - assetStartOffset;
        WriteMaterialParts(stream, mesh.MaterialParts.AsSpan());

        header.meshletOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->meshlets.AsSpan());

        header.meshletGroupOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->groups.AsSpan());

        header.meshletHierarchyNodeOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->hierarchyNodes.AsSpan());

        header.meshletVertexOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->meshletVertices.AsSpan());

        header.meshletTriangleOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->meshletTriangles.AsSpan());

        var endOfAsset = stream.Position;

        // Backpatch header with real offsets
        stream.Position = assetStartOffset;
        stream.Write(header);
        stream.Position = endOfAsset;
        stream.Flush();
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

        stream.Write(MemoryMarshal.AsBytes(buffer));
    }
}
