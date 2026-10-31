using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
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
                    WriteMeshContent(dst, mesh, pMeshletData, meshSettings);
                }
                else
                {
                    var subPath = string.IsNullOrWhiteSpace(mesh.Name) ? $"Mesh_{i}" : mesh.Name;
                    using var subStream = ctx.AddSubAsset(subPath, AssetType.Mesh);
                    WriteMeshContent(subStream, mesh, pMeshletData, meshSettings);
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

    private static void WriteMeshContent(Stream stream, ParsedMesh mesh, MeshletMeshData* meshletData, MeshBakeSettings settings)
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

        // 8. Ray Tracing Proxy Geometry
        using var proxyMesh = BuildRayTracingProxy(mesh, settings, AllocationHandle.Persistent);
        header.rayTracingProxyOffset = stream.Position - assetStartOffset;
        header.rayTracingProxyVertexCount = proxyMesh.positions.Count;
        header.rayTracingProxyIndexCount = proxyMesh.indices.Count;
        stream.Write(proxyMesh.positions.AsSpan());
        stream.Write(proxyMesh.indices.AsSpan());
        header.rayTracingProxySize = stream.Position - assetStartOffset - header.rayTracingProxyOffset;

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

    internal struct RayTracingProxyMesh : IDisposable
    {
        public UnsafeList<float3> positions;
        public UnsafeList<uint> indices;

        public void Dispose()
        {
            positions.Dispose();
            indices.Dispose();
        }
    }

    private static RayTracingProxyMesh BuildRayTracingProxy(ParsedMesh mesh, MeshBakeSettings settings, AllocationHandle allocationHandle)
    {
        var pVertices = (Vertex*)mesh.vertices.GetUnsafePtr();
        var triCount = mesh.indices.Count / 3;

        var percent = Math.Clamp(settings.FallbackPercentage, 0.0f, 1.0f);

        if (percent >= 1.0f || triCount <= 1000)
        {
            var fallbackProxy = new RayTracingProxyMesh
            {
                positions = new UnsafeList<float3>(mesh.vertices.Count, allocationHandle),
                indices = new UnsafeList<uint>(mesh.indices.Count, allocationHandle)
            };
            for (var i = 0; i < mesh.vertices.Count; i++)
            {
                fallbackProxy.positions.Add(pVertices[i].position);
            }
            fallbackProxy.indices.AddRange(mesh.indices.AsSpan());
            return fallbackProxy;
        }

        nuint targetIndexCount;
        float targetError;

        if (percent > 0.0f)
        {
            // Percentage mode (> 0.0): target exact fraction of triangles
            targetIndexCount = (nuint)Math.Max(3, (int)(mesh.indices.Count * percent));
            targetError = 1.0f;
        }
        else
        {
            // Relative error mode (== 0.0): reduce as much as target error allows
            targetIndexCount = 3;
            targetError = settings.FallbackRelativeError > 0.0f ? settings.FallbackRelativeError : 0.01f;
        }

        using var destIndices = new UnsafeArray<uint>(mesh.indices.Count, allocationHandle);
        var resultError = 0.0f;

        var simplifiedIndexCount = MeshOptApi.Simplify(
            (uint*)destIndices.GetUnsafePtr(),
            (uint*)mesh.indices.GetUnsafePtr(),
            (nuint)mesh.indices.Count,
            (float*)&pVertices->position,
            (nuint)mesh.vertices.Count,
            (nuint)sizeof(Vertex),
            targetIndexCount,
            targetError,
            percent > 0.0f ? SimplifyOptions.None : SimplifyOptions.LockBorder,
            &resultError);

        if (simplifiedIndexCount == 0 || simplifiedIndexCount > (nuint)mesh.indices.Count)
        {
            var fallbackProxy = new RayTracingProxyMesh
            {
                positions = new UnsafeList<float3>(mesh.vertices.Count, allocationHandle),
                indices = new UnsafeList<uint>(mesh.indices.Count, allocationHandle)
            };
            for (var i = 0; i < mesh.vertices.Count; i++)
            {
                fallbackProxy.positions.Add(pVertices[i].position);
            }
            fallbackProxy.indices.AddRange(mesh.indices.AsSpan());
            return fallbackProxy;
        }

        using var remap = new UnsafeArray<uint>(mesh.vertices.Count, allocationHandle);
        var numUniqueVertices = (uint)MeshOptApi.GenerateVertexRemap(
            (uint*)remap.GetUnsafePtr(),
            (uint*)destIndices.GetUnsafePtr(),
            simplifiedIndexCount,
            pVertices,
            (nuint)mesh.vertices.Count,
            (nuint)sizeof(Vertex));

        var proxy = new RayTracingProxyMesh
        {
            positions = new UnsafeList<float3>((int)numUniqueVertices, allocationHandle),
            indices = new UnsafeList<uint>((int)simplifiedIndexCount, allocationHandle)
        };
        proxy.positions.UnsafeSetCount((int)numUniqueVertices);
        proxy.indices.UnsafeSetCount((int)simplifiedIndexCount);

        for (var i = 0; i < mesh.vertices.Count; i++)
        {
            var newIdx = remap[i];
            if (newIdx != ~0u && newIdx < numUniqueVertices)
            {
                proxy.positions[(int)newIdx] = pVertices[i].position;
            }
        }

        MeshOptApi.RemapIndexBuffer(
            (uint*)proxy.indices.GetUnsafePtr(),
            (uint*)destIndices.GetUnsafePtr(),
            simplifiedIndexCount,
            (uint*)remap.GetUnsafePtr());

        MeshOptApi.OptimizeVertexCache(
            (uint*)proxy.indices.GetUnsafePtr(),
            (uint*)proxy.indices.GetUnsafePtr(),
            simplifiedIndexCount,
            numUniqueVertices);

        return proxy;
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
