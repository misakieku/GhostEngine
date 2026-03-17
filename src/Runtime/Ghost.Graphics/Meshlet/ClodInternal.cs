using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Meshlet;

internal static class ClodInternal
{
    public static unsafe UnsafeList<Cluster> Clusterize(ClodConfig config, ClodMesh mesh, uint* indices, nuint indexCount, Allocator allocator)
    {
        nuint maxMeshlets = MeshOptApi.BuildMeshletsBound(indexCount, config.maxVertices, config.minTriangles);

        var meshlets = new UnsafeList<meshopt_Meshlet>((int)maxMeshlets, Allocator.Temp);
        meshlets.Resize((int)maxMeshlets);
        var meshletVertices = new UnsafeList<uint>((int)indexCount, Allocator.Temp);
        meshletVertices.Resize((int)indexCount);
        var meshletTriangles = new UnsafeList<byte>((int)indexCount, Allocator.Temp);
        meshletTriangles.Resize((int)indexCount);

        meshopt_Meshlet* pMeshlets = (meshopt_Meshlet*)meshlets.GetUnsafePtr();
        uint* pMeshletVertices = (uint*)meshletVertices.GetUnsafePtr();
        byte* pMeshletTriangles = (byte*)meshletTriangles.GetUnsafePtr();

        nuint meshletCount;
        if (config.clusterSpatial)
        {
            meshletCount = pMeshlets[0].BuildsSpatial(
                pMeshletVertices, pMeshletTriangles,
                indices, indexCount,
                mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride,
                config.maxVertices, config.minTriangles, config.maxTriangles,
                config.clusterFillWeight
            );
        }
        else
        {
            meshletCount = pMeshlets[0].BuildsFlex(
                pMeshletVertices, pMeshletTriangles,
                indices, indexCount,
                mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride,
                config.maxVertices, config.minTriangles, config.maxTriangles,
                0.0f, config.clusterSplitFactor
            );
        }

        var clusters = new UnsafeList<Cluster>((int)meshletCount, allocator);

        for (nuint i = 0; i < meshletCount; i++)
        {
            ref var meshlet = ref pMeshlets[i];

            if (config.optimizeClusters)
            {
                MeshOptApi.OptimizeMeshlet(
                    pMeshletVertices + meshlet.vertex_offset,
                    pMeshletTriangles + meshlet.triangle_offset,
                    meshlet.triangle_count,
                    meshlet.vertex_count
                );
            }

            var cluster = new Cluster
            {
                vertices = meshlet.vertex_count,
                indices = new UnsafeList<uint>((int)(meshlet.triangle_count * 3), allocator),
                group = -1,
                refined = -1
            };

            for (nuint j = 0; j < meshlet.triangle_count * 3; j++)
                cluster.indices.Add(pMeshletVertices[meshlet.vertex_offset + pMeshletTriangles[meshlet.triangle_offset + j]]);

            clusters.Add(cluster);
        }

        meshlets.Dispose();
        meshletVertices.Dispose();
        meshletTriangles.Dispose();

        return clusters;
    }
}
