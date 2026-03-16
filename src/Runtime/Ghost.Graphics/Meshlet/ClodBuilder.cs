using System;
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
    private const float CONST_SIMPLIFY_RATIO_DEFAULT = 0.5f;
    private const float CONST_SIMPLIFY_THRESHOLD_DEFAULT = 0.85f;
    private const float CONST_SIMPLIFY_ERROR_MERGE_PREVIOUS_DEFAULT = 1.0f;
    private const float CONST_SIMPLIFY_ERROR_MERGE_ADDITIVE_DEFAULT = 0.0f;
    private const float CONST_SIMPLIFY_ERROR_FACTOR_SLOPPY_DEFAULT = 2.0f;

    public static nuint Build(ClodConfig config, ClodMesh mesh, void* outputContext, ClodOutputDelegate outputCallback, Allocator allocator = Allocator.Persistent)
    {
        if (mesh.vertexAttributesStride % (nuint)sizeof(float) != 0)
            throw new ArgumentException("vertexAttributesStride must be a multiple of sizeof(float)");

        var locks = new UnsafeList<byte>((int)mesh.vertexCount, allocator);
        locks.Resize(mesh.vertexCount);
        for (int i = 0; i < (int)mesh.vertexCount; i++)
            locks[i] = 0;

        // Generate position-only remap
        var remap = new UnsafeList<uint>((int)mesh.vertexCount, allocator);
        remap.Resize(mesh.vertexCount);
        Api.meshopt_generatePositionRemap(remap.Ptr, mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);

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
                            locks[(int)i] |= (byte)Api.meshopt_SimplifyVertex_Protect;
                        }
                    }
                }
            }
        }

        // Initial clusterization
        var clusters = ClodInternal.Clusterize(config, mesh, mesh.indices, mesh.indexCount, allocator);

        // Compute initial bounds
        for (int i = 0; i < (int)clusters.Length; i++)
        {
            clusters[i].bounds = ClodBoundsHelper.ComputeBounds(mesh, clusters[i].indices, 0.0f);
        }

        var pending = new UnsafeList<int>((int)clusters.Length, allocator);
        pending.Resize((nuint)clusters.Length);
        for (int i = 0; i < (int)clusters.Length; i++)
            pending[i] = i;

        int depth = 0;

        while (pending.Length > 1)
        {
            var groups = ClodInternal.Partition(config, mesh, clusters, pending, remap, allocator);

            pending.Clear();

            // Lock boundaries
            ClodInternal.LockBoundary(locks, groups, clusters, remap, mesh.vertexLock);

            for (int i = 0; i < (int)groups.Length; i++)
            {
                var merged = new UnsafeList<uint>(groups[i].Length * (int)config.MaxTriangles * 3, allocator);
                for (int j = 0; j < (int)groups[i].Length; j++)
                {
                    var clusterIndices = clusters[groups[i][j]].indices;
                    for (int k = 0; k < (int)clusterIndices.Length; k++)
                        merged.Add(clusterIndices[k]);
                }

                nuint targetSize = ((nuint)merged.Length / 3) * (nuint)config.SimplifyRatio * 3;

                var bounds = ClodBoundsHelper.MergeBounds(clusters, groups[i]);

                float error = 0.0f;
                var simplified = ClodSimplify.Simplify(config, mesh, merged, locks, targetSize, &error, allocator);

                if (simplified.Length > (nuint)(merged.Length * config.SimplifyThreshold))
                {
                    bounds.error = float.MaxValue;
                    OutputGroup(config, mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback, allocator);
                    merged.Dispose();
                    simplified.Dispose();
                    continue;
                }

                bounds.error = Math.Max(bounds.error * config.SimplifyErrorMergePrevious, error) + error * config.SimplifyErrorMergeAdditive;

                int refined = OutputGroup(config, mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback, allocator);

                // Discard old clusters
                for (int j = 0; j < (int)groups[i].Length; j++)
                {
                    clusters[groups[i][j]].indices.Dispose();
                }

                // Clusterize simplified mesh
                var split = ClodInternal.Clusterize(config, mesh, simplified.Ptr, simplified.Length, allocator);
                for (int j = 0; j < (int)split.Length; j++)
                {
                    split[j].refined = refined;
                    split[j].bounds = bounds;
                    clusters.Add(split[j]);
                    pending.Add((int)clusters.Length - 1);
                }

                split.Dispose();
                merged.Dispose();
                simplified.Dispose();
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
            OutputGroup(config, mesh, clusters, pending, bounds, depth, outputContext, outputCallback, allocator);
        }

        // Cleanup
        for (int i = 0; i < (int)clusters.Length; i++)
            clusters[i].indices.Dispose();
        clusters.Dispose();
        locks.Dispose();
        remap.Dispose();
        pending.Dispose();

        return (nuint)clusters.Length;
    }

    private static int OutputGroup(
        ClodConfig config,
        ClodMesh mesh,
        UnsafeList<Cluster> clusters,
        UnsafeList<int> group,
        ClodBounds simplified,
        int depth,
        void* outputContext,
        ClodOutputDelegate outputCallback,
        Allocator allocator
    )
    {
        var groupClusters = new UnsafeList<ClodCluster>((int)group.Length, allocator);
        groupClusters.Resize((nuint)group.Length);

        for (int i = 0; i < (int)group.Length; i++)
        {
            ref var srcCluster = ref clusters[group[i]];
            ref var dstCluster = ref groupClusters[i];

            dstCluster.refined = srcCluster.refined;
            dstCluster.bounds = (config.OptimizeBounds && srcCluster.refined != -1)
                ? ClodBoundsHelper.ComputeBounds(mesh, srcCluster.indices, srcCluster.bounds.error)
                : srcCluster.bounds;
            dstCluster.indices = srcCluster.indices.Ptr;
            dstCluster.indexCount = (nuint)srcCluster.indices.Length;
            dstCluster.vertexCount = srcCluster.vertices;
        }

        var clodGroup = new ClodGroup { Depth = depth, Simplified = simplified };
        int result = outputCallback != null
            ? outputCallback(outputContext, clodGroup, (ClodCluster*)groupClusters.Ptr, (nuint)groupClusters.Length)
            : -1;

        groupClusters.Dispose();
        return result;
    }
}
