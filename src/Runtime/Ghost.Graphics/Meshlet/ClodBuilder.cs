using System;
using System.Diagnostics;
using System.Numerics;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance;

namespace Ghost.Graphics.Meshlet;

internal struct Cluster
{
    public nuint vertices;
    public UnsafeList<uint> indices;
    public int group;
    public int refined;
    public ClodBounds bounds;
}

public unsafe static class ClodBuilder
{
    public static nuint Build(ClodConfig config, ClodMesh mesh, void* outputContext, ClodOutputDelegate outputCallback)
    {
        Debug.Assert(mesh.vertexAttributesStride % (nuint)sizeof(float) == 0, "vertexAttributesStride must be a multiple of sizeof(float)");

        // Use Persistent or Temp for large mesh data to avoid stack overflow
        var locks = new UnsafeList<byte>(mesh.vertexCount, Allocator.Temp);
        locks.AsSpan().Fill(0);

        // Generate position-only remap
        var remap = new UnsafeList<uint>(mesh.vertexCount, Allocator.Temp);
        MeshOptApi.GeneratePositionRemap(remap.GetUnsafePtr(), mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);

        // Set up protect bits on UV seams
        if (mesh.attributeProtectMask != 0)
        {
            nuint maxAttributes = mesh.vertexAttributesStride / sizeof(float);
            for (nuint i = 0; i < mesh.vertexCount; i++)
            {
                uint r = remap[(int)i];
                for (nuint j = 0; j < maxAttributes; j++)
                {
                    if ((r != i) && ((mesh.attributeProtectMask & (1u << (int)j)) != 0))
                    {
                        if (mesh.vertexAttributes[i * maxAttributes + j] != mesh.vertexAttributes[r * maxAttributes + j])
                        {
                            locks[(int)i] |= (byte)MeshOptApi.SimplifyVertex_Protect;
                        }
                    }
                }
            }
        }

        // Initial clusterization
        var clusters = ClodInternal.Clusterize(config, mesh, mesh.indices, mesh.indexCount, Allocator.Persistent);

        // Compute initial bounds
        for (int i = 0; i < (int)clusters.Length; i++)
        {
            clusters[i].bounds = ClodBoundsHelper.ComputeBounds(mesh, clusters[i].indices, 0.0f);
        }

        var pending = new UnsafeList<int>(clusters.Length, Allocator.Temp);
        for (int i = 0; i < (int)clusters.Length; i++)
            pending.Add(i);

        int depth = 0;

        while (pending.Length > 1)
        {
            // Partition results are temporary but returned, using Temp allocator
            var groups = ClodPartition.Partition(config, mesh, clusters, pending, remap, Allocator.Temp);

            pending.Clear();

            // Lock boundaries
            ClodBoundary.LockBoundary(locks, groups, clusters, remap, mesh.vertexLock);

            for (int i = 0; i < (int)groups.Length; i++)
            {
                // Merged indices for a group of clusters
                var merged = new UnsafeList<uint>((nuint)groups[i].Length * config.maxTriangles * 3, Allocator.Temp);
                for (int j = 0; j < (int)groups[i].Length; j++)
                {
                    var clusterIndices = clusters[groups[i][j]].indices;
                    for (int k = 0; k < (int)clusterIndices.Length; k++)
                        merged.Add(clusterIndices[k]);
                }

                nuint targetSize = ((nuint)merged.Length / 3) * (nuint)config.simplifyRatio * 3;

                var bounds = ClodBoundsHelper.MergeBounds(clusters, groups[i]);

                float error = 0.0f;
                var simplified = ClodSimplify.Simplify(config, mesh, merged, locks, targetSize, &error);

                if (simplified.Length > (nuint)(merged.Length * config.simplifyThreshold))
                {
                    bounds.error = float.MaxValue;
                    OutputGroup(config, mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback);
                    merged.Dispose();
                    continue;
                }

                bounds.error = Math.Max(bounds.error * config.simplifyErrorMergePrevious, error) + error * config.simplifyErrorMergeAdditive;

                int refined = OutputGroup(config, mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback);

                // Discard old clusters
                for (int j = 0; j < (int)groups[i].Length; j++)
                {
                    clusters[groups[i][j]].indices.Dispose();
                }

                // Clusterize simplified mesh
                var split = ClodInternal.Clusterize(config, mesh, simplified.GetUnsafePtr(), simplified.Length, Allocator.Persistent);
                for (int j = 0; j < (int)split.Length; j++)
                {
                    split[j].refined = refined;
                    split[j].bounds = bounds;
                    clusters.Add(split[j]);
                    pending.Add((int)clusters.Length - 1);
                }

                split.Dispose();
                merged.Dispose();
            }

            // Cleanup groups
            for (int i = 0; i < (int)groups.Length; i++)
                groups[i].Dispose();
            groups.Dispose();

            depth++;
        }

        if (pending.Length > 0)
        {
            var cluster = clusters[pending[0]];
            var bounds = cluster.bounds;
            bounds.error = float.MaxValue;
            OutputGroup(config, mesh, clusters, pending, bounds, depth, outputContext, outputCallback);
        }

        nuint finalClusterCount = clusters.Length;

        // Cleanup
        for (int i = 0; i < (int)clusters.Length; i++)
            clusters[i].indices.Dispose();
        clusters.Dispose();
        
        locks.Dispose();
        remap.Dispose();
        pending.Dispose();

        return finalClusterCount;
    }

    private static int OutputGroup(
        ClodConfig config,
        ClodMesh mesh,
        UnsafeList<Cluster> clusters,
        UnsafeList<int> group,
        ClodBounds simplified,
        int depth,
        void* outputContext,
        ClodOutputDelegate outputCallback
    )
    {
        // Use Temp for the output array to avoid stack pressure
        var groupClusters = new UnsafeList<ClodCluster>(group.Length, Allocator.Temp);

        for (int i = 0; i < (int)group.Length; i++)
        {
            ref var srcCluster = ref clusters[group[i]];
            var dstCluster = new ClodCluster
            {
                refined = srcCluster.refined,
                bounds = (config.optimizeBounds && srcCluster.refined != -1)
                    ? ClodBoundsHelper.ComputeBounds(mesh, srcCluster.indices, srcCluster.bounds.error)
                    : srcCluster.bounds,
                indices = srcCluster.indices.GetUnsafePtr(),
                indexCount = (nuint)srcCluster.indices.Length,
                vertexCount = srcCluster.vertices
            };
            groupClusters.Add(dstCluster);
        }

        var clodGroup = new ClodGroup { depth = depth, simplified = simplified };
        int result = outputCallback != null
            ? outputCallback(outputContext, clodGroup, (ClodCluster*)groupClusters.GetUnsafePtr(), (nuint)groupClusters.Length)
            : -1;

        groupClusters.Dispose();
        return result;
    }
}
