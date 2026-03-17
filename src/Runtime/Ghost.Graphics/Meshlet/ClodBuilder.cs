using System;
using System.Diagnostics;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

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

        var locks = new UnsafeList<byte>((int)mesh.vertexCount, Allocator.Temp);
        locks.AsSpan().Fill(0);

        var remap = new UnsafeList<uint>((int)mesh.vertexCount, Allocator.Temp);
        remap.Resize((int)mesh.vertexCount);
        MeshOptApi.GeneratePositionRemap((uint*)remap.GetUnsafePtr(), mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);

        if (mesh.attributeProtectMask != 0)
        {
            nuint maxAttributes = mesh.vertexAttributesStride / sizeof(float);
            for (nuint i = 0; i < mesh.vertexCount; i++)
            {
                uint r = ((uint*)remap.GetUnsafePtr())[(int)i];
                for (nuint j = 0; j < maxAttributes; j++)
                {
                    if ((r != i) && ((mesh.attributeProtectMask & (1u << (int)j)) != 0))
                    {
                        if (mesh.vertexAttributes[i * maxAttributes + j] != mesh.vertexAttributes[r * maxAttributes + j])
                        {
                            ((byte*)locks.GetUnsafePtr())[(int)i] |= (byte)(Api.meshopt_SimplifyVertex_Protect & 0xFF);
                        }
                    }
                }
            }
        }

        var clusters = ClodInternal.Clusterize(config, mesh, mesh.indices, mesh.indexCount, Allocator.Persistent);

        for (int i = 0; i < clusters.Count; i++)
        {
            clusters[i].bounds = ClodBoundsHelper.ComputeBounds(mesh, clusters[i].indices, 0.0f);
        }

        var pending = new UnsafeList<int>(clusters.Count, Allocator.Temp);
        for (int i = 0; i < clusters.Count; i++)
            pending.Add(i);

        int depth = 0;

        while (pending.Count > 1)
        {
            var groups = ClodPartition.Partition(config, mesh, clusters, pending, remap, Allocator.Temp);
            pending.Clear();

            ClodBoundary.LockBoundary(locks, groups, clusters, remap, mesh.vertexLock);

            for (int i = 0; i < groups.Count; i++)
            {
                var merged = new UnsafeList<uint>(groups[i].Count * (int)config.maxTriangles * 3, Allocator.Temp);
                for (int j = 0; j < groups[i].Count; j++)
                {
                    var clusterIndices = clusters[groups[i][j]].indices;
                    for (int k = 0; k < clusterIndices.Count; k++)
                        merged.Add(clusterIndices[k]);
                }

                nuint targetSize = ((nuint)merged.Count / 3) * (nuint)config.simplifyRatio * 3;
                var bounds = ClodBoundsHelper.MergeBounds(clusters, groups[i]);

                float error = 0.0f;
                var simplified = ClodSimplify.Simplify(config, mesh, merged, locks, targetSize, &error);

                if ((nuint)simplified.Count > (nuint)(merged.Count * config.simplifyThreshold))
                {
                    bounds.error = float.MaxValue;
                    OutputGroup(config, mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback);
                    merged.Dispose();
                    continue;
                }

                bounds.error = Math.Max(bounds.error * config.simplifyErrorMergePrevious, error) + error * config.simplifyErrorMergeAdditive;

                int refined = OutputGroup(config, mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback);

                for (int j = 0; j < groups[i].Count; j++)
                    clusters[groups[i][j]].indices.Dispose();

                var split = ClodInternal.Clusterize(config, mesh, (uint*)simplified.GetUnsafePtr(), (nuint)simplified.Count, Allocator.Persistent);
                for (int j = 0; j < split.Count; j++)
                {
                    split[j].refined = refined;
                    split[j].bounds = bounds;
                    clusters.Add(split[j]);
                    pending.Add(clusters.Count - 1);
                }

                split.Dispose();
                merged.Dispose();
            }

            for (int i = 0; i < groups.Count; i++)
                groups[i].Dispose();
            groups.Dispose();

            depth++;
        }

        if (pending.Count > 0)
        {
            var bounds = clusters[pending[0]].bounds;
            bounds.error = float.MaxValue;
            OutputGroup(config, mesh, clusters, pending, bounds, depth, outputContext, outputCallback);
        }

        nuint finalClusterCount = (nuint)clusters.Count;

        for (int i = 0; i < clusters.Count; i++)
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
        var groupClusters = new UnsafeList<ClodCluster>(group.Count, Allocator.Temp);

        for (int i = 0; i < group.Count; i++)
        {
            ref var srcCluster = ref clusters[group[i]];
            groupClusters.Add(new ClodCluster
            {
                refined = srcCluster.refined,
                bounds = (config.optimizeBounds && srcCluster.refined != -1)
                    ? ClodBoundsHelper.ComputeBounds(mesh, srcCluster.indices, srcCluster.bounds.error)
                    : srcCluster.bounds,
                indices = (uint*)srcCluster.indices.GetUnsafePtr(),
                indexCount = (nuint)srcCluster.indices.Count,
                vertexCount = srcCluster.vertices
            });
        }

        var clodGroup = new ClodGroup { depth = depth, simplified = simplified };
        int result = outputCallback != null
            ? outputCallback(outputContext, clodGroup, (ClodCluster*)groupClusters.GetUnsafePtr(), (nuint)groupClusters.Count)
            : -1;

        groupClusters.Dispose();
        return result;
    }
}
