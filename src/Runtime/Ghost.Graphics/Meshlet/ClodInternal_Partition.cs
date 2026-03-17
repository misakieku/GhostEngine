using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance;

namespace Ghost.Graphics.Meshlet;

internal static class ClodPartition
{
    public static UnsafeList<UnsafeList<int>> Partition(ClodConfig config, ClodMesh mesh, UnsafeList<Cluster> clusters, UnsafeList<int> pending, UnsafeList<uint> remap, AllocationHandle allocator)
    {
        if (pending.Length <= (int)config.partitionSize)
        {
            var partitions = new UnsafeList<UnsafeList<int>>(1, allocator);
            partitions.Add(pending);
            return partitions;
        }

        using var stackScope = AllocationManager.CreateStackScope();
        var clusterIndices = new UnsafeList<uint>(1024, stackScope.AllocationHandle);
        var clusterCounts = new UnsafeList<uint>((nuint)pending.Length, stackScope.AllocationHandle);

        nuint totalIndexCount = 0;
        for (int i = 0; i < pending.Length; i++)
        {
            var cluster = clusters[pending[i]];
            totalIndexCount += cluster.indices.Length;
        }

        clusterIndices.Resize(totalIndexCount);

        nuint offset = 0;
        for (int i = 0; i < pending.Length; i++)
        {
            var cluster = clusters[pending[i]];
            clusterCounts.Add((uint)cluster.indices.Length);

            for (int j = 0; j < (int)cluster.indices.Length; j++)
            {
                clusterIndices[(int)offset + j] = remap[(int)cluster.indices[j]];
            }
            offset += (nuint)cluster.indices.Length;
        }

        var clusterPart = new UnsafeList<uint>((nuint)pending.Length, stackScope.AllocationHandle);
        clusterPart.Resize((nuint)pending.Length);

        nuint partitionCount = MeshOptApi.PartitionClusters(
            clusterPart.GetUnsafePtr(),
            clusterIndices.GetUnsafePtr(),
            totalIndexCount,
            clusterCounts.GetUnsafePtr(),
            (nuint)pending.Length,
            config.partitionSpatial ? mesh.vertexPositions : null,
            remap.Length,
            mesh.vertexPositionsStride,
            config.partitionSize
        );

        var partitions = new UnsafeList<UnsafeList<int>>(partitionCount, allocator);
        for (nuint i = 0; i < partitionCount; i++)
        {
            partitions.Add(new UnsafeList<int>((nuint)(config.partitionSize + config.partitionSize / 3), allocator));
        }

        for (int i = 0; i < pending.Length; i++)
        {
            partitions[(int)clusterPart[i]].Add(pending[i]);
        }

        return partitions;
    }
}
