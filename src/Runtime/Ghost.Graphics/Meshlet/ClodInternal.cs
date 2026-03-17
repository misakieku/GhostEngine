using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance;

namespace Ghost.Graphics.Meshlet;

internal static class ClodInternal
{
    public static UnsafeList<Cluster> Clusterize(ClodConfig config, ClodMesh mesh, uint* indices, nuint indexCount, Allocator allocator)
    {
        nuint maxMeshlets = MeshOptApi.BuildMeshletsBound(indexCount, config.maxVertices, config.minTriangles);

        var meshlets = new UnsafeList<meshopt_Meshlet>(maxMeshlets, allocator);
        var meshletVertices = new UnsafeList<uint>(indexCount, allocator);
        var meshletTriangles = new UnsafeList<byte>(indexCount, allocator);

        meshlets.Resize(maxMeshlets);
        
        nuint meshletCount;
        if (config.clusterSpatial)
        {
            meshletCount = MeshOptApi.BuildMeshletsSpatial(
                meshlets.GetUnsafePtr(), 
                meshletVertices.GetUnsafePtr(), 
                meshletTriangles.GetUnsafePtr(), 
                indices, 
                indexCount,
                mesh.vertexPositions, 
                mesh.vertexCount, 
                mesh.vertexPositionsStride,
                config.maxVertices, 
                config.minTriangles, 
                config.maxTriangles, 
                config.clusterFillWeight
            );
        }
        else
        {
            meshletCount = MeshOptApi.BuildMeshletsFlex(
                meshlets.GetUnsafePtr(), 
                meshletVertices.GetUnsafePtr(), 
                meshletTriangles.GetUnsafePtr(), 
                indices, 
                indexCount,
                mesh.vertexPositions, 
                mesh.vertexCount, 
                mesh.vertexPositionsStride,
                config.maxVertices, 
                config.minTriangles, 
                config.maxTriangles, 
                0.0f, 
                config.clusterSplitFactor
            );
        }
        meshlets.Resize(meshletCount);

        var clusters = new UnsafeList<Cluster>(meshletCount, allocator);
        
        for (nuint i = 0; i < meshletCount; i++)
        {
            ref var meshlet = ref meshlets[i];

            if (config.optimizeClusters)
            {
                MeshOptApi.OptimizeMeshlet(
                    meshletVertices.GetUnsafePtr() + meshlet.vertexOffset, 
                    meshletTriangles.GetUnsafePtr() + meshlet.triangleOffset, 
                    meshlet.triangleCount, 
                    meshlet.vertexCount
                );
            }

            var cluster = new Cluster
            {
                vertices = meshlet.vertexCount,
                indices = new UnsafeList<uint>(meshlet.triangleCount * 3, allocator),
                group = -1,
                refined = -1
            };

            for (nuint j = 0; j < meshlet.triangleCount * 3; j++)
            {
                cluster.indices.Add(meshletVertices[(int)(meshlet.vertexOffset + meshletTriangles[(int)(meshlet.triangleOffset + j)])]);
            }

            clusters.Add(cluster);
        }

        // Cleanup
        meshlets.Dispose();
        meshletVertices.Dispose();
        meshletTriangles.Dispose();

        return clusters;
    }
}
