using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Core.Bakers;

[AssetBaker(Extensions = [".fbx", ".obj"], Type = AssetType.Mesh, SettingsType = typeof(MeshBakeSettings), SettingsVersion = MeshBakeSettings.VERSION)]
public unsafe partial class MeshBaker : IAssetBaker
{
    public bool CompressOutput => false;

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
                    mesh.vertices.AsReadOnly(),
                    mesh.indices.AsReadOnly(),
                    mesh.materialParts.AsReadOnly(),
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

        // Bundle meshlets, local packed vertices, and micro-triangles into 128KB pages
        using var packedPages = PagePacker.PackPages(meshletData, AllocationHandle.Persistent);

        var pageDirectory = packedPages.pageDirectory;
        var pageBuffers = packedPages.pageBuffers;
        var reorderedMeshlets = packedPages.reorderedMeshlets;

        var header = new MeshContentHeader
        {
            magic = MeshContentHeader.MAGIC,
            version = MeshContentHeader.VERSION,
            meshletCount = reorderedMeshlets.Count,
            meshletGroupCount = meshletData->groups.Count,
            meshletHierarchyNodeCount = meshletData->hierarchyNodes.Count,
            materialSlotCount = meshletData->materialSlotCount,
            lodLevelCount = meshletData->lodLevelCount,
            pageCount = pageDirectory.Count,
            rootPageCount = pageDirectory.Count > 0 && pageDirectory[0].isRootPage != 0 ? 1 : 0,
            rootMeshletCount = pageDirectory.Count > 0 && pageDirectory[0].isRootPage != 0 ? pageDirectory[0].meshletCount : 0,
            boundsMin = mesh.boundingBox.Min,
            boundsMax = mesh.boundingBox.Max,
            boundingSphereCenter = (mesh.boundingBox.Min + mesh.boundingBox.Max) * 0.5f,
            boundingSphereRadius = Misaki.HighPerformance.Mathematics.math.length(mesh.boundingBox.Max - (mesh.boundingBox.Min + mesh.boundingBox.Max) * 0.5f),
        };

        // Write header placeholder
        stream.Write(header);

        // 1. Hierarchy DAG nodes
        header.hierarchyOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->hierarchyNodes.AsSpan());
        header.hierarchySize = stream.Position - assetStartOffset - header.hierarchyOffset;

        // 2. Meshlet Groups
        header.groupOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->groups.AsSpan());
        header.groupSize = stream.Position - assetStartOffset - header.groupOffset;

        // 3. Reordered Meshlets table
        header.meshletOffset = stream.Position - assetStartOffset;
        stream.Write(reorderedMeshlets.AsSpan());
        header.meshletSize = stream.Position - assetStartOffset - header.meshletOffset;

        // 4. Page Directory Table placeholder
        var pageDirStartPos = stream.Position;
        header.pageDirectoryOffset = stream.Position - assetStartOffset;
        stream.Write(pageDirectory.AsSpan());
        header.pageDirectorySize = stream.Position - assetStartOffset - header.pageDirectoryOffset;

        // 5. Root Page 0 (uncompressed payload)
        if (pageBuffers.Length > 0 && pageDirectory[0].isRootPage != 0)
        {
            header.rootPageOffset = stream.Position - assetStartOffset;
            stream.Write(pageBuffers[0]);
            header.rootPageSize = stream.Position - assetStartOffset - header.rootPageOffset;

            ref var p0 = ref pageDirectory[0];
            p0.fileOffset = (ulong)header.rootPageOffset;
        }

        // 6. Bulk Pages 1..N
        if (pageBuffers.Length > 1)
        {
            header.bulkPagesOffset = stream.Position - assetStartOffset;
            for (var p = 1; p < pageBuffers.Length; p++)
            {
                ref var dirEntry = ref pageDirectory[p];
                dirEntry.fileOffset = (ulong)(stream.Position - assetStartOffset);
                stream.Write(pageBuffers[p]);
                dirEntry.compressedSize = (uint)pageBuffers[p].Length;
                dirEntry.uncompressedSize = (uint)pageBuffers[p].Length;
            }
            header.bulkPagesSize = stream.Position - assetStartOffset - header.bulkPagesOffset;
        }

        // 7. Material Parts
        header.materialPartOffset = stream.Position - assetStartOffset;
        WriteMaterialParts(stream, mesh.materialParts.AsSpan());
        header.materialPartSize = stream.Position - assetStartOffset - header.materialPartOffset;

        var endOfAsset = stream.Position;

        // Backpatch Page Directory with real file offsets
        stream.Position = pageDirStartPos;
        stream.Write(pageDirectory.AsSpan());

        // Backpatch Header with all real offsets and sizes
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
