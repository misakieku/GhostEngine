using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance;

namespace Ghost.Graphics.Meshlet;

internal static class ClodInternal
{
    public static UnsafeList<Cluster> Clusterize(ClodConfig config, ClodMesh mesh, uint* indices, nuint indexCount, Allocator allocator)
    {
        nuint maxMeshlets = Api.meshopt_buildMeshletsBound(indexCount, config.MaxVertices, config.MinTriangles);

        var meshlets = new UnsafeList<meshopt_Meshlet>(maxMeshlets, allocator);
        var meshletVertices = new UnsafeList<uint>(indexCount, allocator);
        var meshletTriangles = new UnsafeList<byte>(indexCount, allocator);

        meshlets.Resize(maxMeshlets);
        
        nuint meshletCount;
        if (config.ClusterSpatial)
        {
            meshletCount = Api.meshopt_buildMeshletsSpatial(
                meshlets.Ptr, 
                meshletVertices.Ptr, 
                meshletTriangles.Ptr, 
                indices, 
                indexCount,
                mesh.vertexPositions, 
                mesh.vertexCount, 
                mesh.vertexPositionsStride,
                config.MaxVertices, 
                config.MinTriangles, 
                config.MaxTriangles, 
                config.ClusterFillWeight
            );
        }
        else
        {
            meshletCount = Api.meshopt_buildMeshletsFlex(
                meshlets.Ptr, 
                meshletVertices.Ptr, 
                meshletTriangles.Ptr, 
                indices, 
                indexCount,
                mesh.vertexPositions, 
                mesh.vertexCount, 
                mesh.vertexPositionsStride,
                config.MaxVertices, 
                config.MinTriangles, 
                config.MaxTriangles, 
                0.0f, 
                config.ClusterSplitFactor
            );
        }
        meshlets.Resize(meshletCount);

        var clusters = new UnsafeList<Cluster>(meshletCount, allocator);
        
        for (nuint i = 0; i < meshletCount; i++)
        {
            ref var meshlet = ref meshlets[i];

            if (config.OptimizeClusters)
            {
                Api.meshopt_optimizeMeshlet(
                    meshletVertices.Ptr + meshlet.vertexOffset, 
                    meshletTriangles.Ptr + meshlet.triangleOffset, 
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
                cluster.indices.Add(meshletVertices[meshlet.vertexOffset + meshletTriangles[meshlet.triangleOffset + j]]);
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
