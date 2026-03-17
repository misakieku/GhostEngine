using System;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Meshlet;

internal static class ClodPartition
{
    public static unsafe UnsafeList<UnsafeList<int>> Partition(ClodConfig config, ClodMesh mesh, UnsafeList<Cluster> clusters, UnsafeList<int> pending, UnsafeList<uint> remap, Allocator allocator)
    {
        if (pending.Count <= (int)config.partitionSize)
        {
            var single = new UnsafeList<UnsafeList<int>>(1, allocator);
            single.Add(pending);
            return single;
        }

        nuint totalIndexCount = 0;
        for (int i = 0; i < pending.Count; i++)
            totalIndexCount += (nuint)clusters[pending[i]].indices.Count;

        var clusterIndices = new UnsafeList<uint>((int)totalIndexCount, Allocator.Temp);
        var clusterCounts = new UnsafeList<uint>(pending.Count, Allocator.Temp);

        nuint offset = 0;
        for (int i = 0; i < pending.Count; i++)
        {
            var cluster = clusters[pending[i]];
            clusterCounts.Add((uint)cluster.indices.Count);
            for (int j = 0; j < cluster.indices.Count; j++)
                clusterIndices.Add(((uint*)remap.GetUnsafePtr())[(int)cluster.indices[j]]);
            offset += (nuint)cluster.indices.Count;
        }

        var clusterPart = new UnsafeList<uint>(pending.Count, Allocator.Temp);
        clusterPart.Resize(pending.Count);

        nuint partitionCount = MeshOptApi.PartitionClusters(
            (uint*)clusterPart.GetUnsafePtr(),
            (uint*)clusterIndices.GetUnsafePtr(),
            totalIndexCount,
            (uint*)clusterCounts.GetUnsafePtr(),
            (nuint)pending.Count,
            config.partitionSpatial ? mesh.vertexPositions : null,
            (nuint)remap.Count,
            mesh.vertexPositionsStride,
            config.partitionSize
        );

        var partitions = new UnsafeList<UnsafeList<int>>((int)partitionCount, allocator);
        for (nuint i = 0; i < partitionCount; i++)
            partitions.Add(new UnsafeList<int>((int)(config.partitionSize + config.partitionSize / 3), allocator));

        for (int i = 0; i < pending.Count; i++)
            partitions[(int)((uint*)clusterPart.GetUnsafePtr())[i]].Add(pending[i]);

        clusterIndices.Dispose();
        clusterCounts.Dispose();
        clusterPart.Dispose();

        return partitions;
    }
}
