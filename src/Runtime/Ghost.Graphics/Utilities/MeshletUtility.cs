using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Diagnostics;

namespace Ghost.Graphics.Utilities;

internal struct Cluster : IDisposable
{
    public UnsafeList<uint> indices;
    public ClodBounds bounds;
    public nuint vertices;
    public int group;
    public int refined;

    public void Dispose()
    {
        indices.Dispose();
    }
}

/// <summary>
/// Represents the bounding sphere and simplification error for a LOD cluster.
/// </summary>
public struct ClodBounds
{
    /// <summary> The center of the bounding sphere. </summary>
    public float3 center;
    /// <summary> The radius of the bounding sphere. </summary>
    public float radius;
    /// <summary> The simplification error associated with this LOD level. </summary>
    public float error;
}

/// <summary>
/// Configuration parameters for the cluster LOD generation pipeline.
/// </summary>
public struct ClodConfig
{
    /// <summary> The maximum number of vertices per meshlet. </summary>
    public nuint maxVertices;
    /// <summary> The minimum number of triangles per meshlet. </summary>
    public nuint minTriangles;
    /// <summary> The maximum number of triangles per meshlet. </summary>
    public nuint maxTriangles;
    /// <summary> Whether to use spatial partitioning during meshlet building. </summary>
    public bool partitionSpatial;
    /// <summary> Whether to sort clusters after partitioning. </summary>
    public bool partitionSort;
    /// <summary> The target size for partitions. </summary>
    public nuint partitionSize;
    /// <summary> Whether to cluster meshlets using spatial clustering. </summary>
    public bool clusterSpatial;
    /// <summary> Weight factor for cluster fill calculation. </summary>
    public float clusterFillWeight;
    /// <summary> Split factor for flexible clustering. </summary>
    public float clusterSplitFactor;
    /// <summary> The simplification ratio to achieve per LOD level. </summary>
    public float simplifyRatio;
    /// <summary> Threshold for stopping simplification. </summary>
    public float simplifyThreshold;
    /// <summary> Error factor used when merging previous LOD level errors. </summary>
    public float simplifyErrorMergePrevious;
    /// <summary> Additive error factor when merging LOD levels. </summary>
    public float simplifyErrorMergeAdditive;
    /// <summary> Error factor for sloppy simplification. </summary>
    public float simplifyErrorFactorSloppy;
    /// <summary> Edge length limit error factor. </summary>
    public float simplifyErrorEdgeLimit;
    /// <summary> Whether to allow permissive simplification. </summary>
    public bool simplifyPermissive;
    /// <summary> Whether to fallback to permissive simplification. </summary>
    public bool simplifyFallbackPermissive;
    /// <summary> Whether to fallback to sloppy simplification. </summary>
    public bool simplifyFallbackSloppy;
    /// <summary> Whether to regularize the mesh during simplification. </summary>
    public bool simplifyRegularize;
    /// <summary> Whether to optimize cluster bounds. </summary>
    public bool optimizeBounds;
    /// <summary> Whether to optimize clusters post-build. </summary>
    public bool optimizeClusters;
}

/// <summary>
/// Contains input data for the Cluster LOD generation pipeline.
/// </summary>
public unsafe struct ClodMesh
{
    /// <summary> Pointer to vertex position data (float array). </summary>
    public float* vertexPositions;
    /// <summary> Number of vertices in the mesh. </summary>
    public nuint vertexCount;
    /// <summary> Stride in bytes for vertex position data. </summary>
    public nuint vertexPositionsStride;
    /// <summary> Pointer to vertex attribute data (float array). </summary>
    public float* vertexAttributes;
    /// <summary> Stride in bytes for vertex attribute data. </summary>
    public nuint vertexAttributesStride;
    /// <summary> Pointer to attribute weights for simplification. </summary>
    public float* attributeWeights;
    /// <summary> Number of vertex attributes. </summary>
    public nuint attributeCount;
    /// <summary> Pointer to index data. </summary>
    public uint* indices;
    /// <summary> Number of indices in the mesh. </summary>
    public nuint indexCount;
    /// <summary> Pointer to per-vertex lock flags (1 byte per vertex). </summary>
    public byte* vertexLock;
    /// <summary> Mask indicating which attributes are protected during simplification. </summary>
    public uint attributeProtectMask;
}

/// <summary>
/// Defines a group of clusters in the LOD hierarchy.
/// </summary>
public struct ClodGroup
{
    /// <summary> LOD hierarchy depth of this group. </summary>
    public int depth;
    /// <summary> Bounding information for the simplified group. </summary>
    public ClodBounds simplified;
}

/// <summary>
/// Represents a cluster of meshlets in the LOD hierarchy.
/// </summary>
public unsafe struct ClodCluster
{
    /// <summary> Refinement level of the cluster. </summary>
    public int refined;
    /// <summary> Bounding info for the cluster. </summary>
    public ClodBounds bounds;
    /// <summary> Pointer to indices for this cluster. </summary>
    public uint* indices;
    /// <summary> Number of indices. </summary>
    public nuint indexCount;
    /// <summary> Number of vertices in the cluster. </summary>
    public nuint vertexCount;
}

/// <summary>
/// Delegate type for processing generated LOD groups.
/// </summary>
public unsafe delegate int ClodOutputDelegate(void* context, ClodGroup group, ReadOnlyUnsafeCollection<ClodCluster> clusters);

// FIX: UnsafeList and UnsafeArray are not same as std::vector.

public static unsafe class MeshletUtility
{
    private static ClodBounds ComputeBounds(ClodMesh mesh, UnsafeList<uint> indices, float error)
    {
        var bounds = MeshOptApi.ComputeClusterBounds((uint*)indices.GetUnsafePtr(), (nuint)indices.Count, mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);
        return new ClodBounds
        {
            center = new float3(bounds.center[0], bounds.center[1], bounds.center[2]),
            radius = bounds.radius,
            error = error
        };
    }

    private static ClodBounds MergeBounds(UnsafeList<Cluster> clusters, UnsafeList<int> group)
    {
        using var boundsList = new UnsafeArray<ClodBounds>(group.Count, Allocator.FreeList);
        for (var j = 0; j < group.Count; j++)
        {
            boundsList[j] = (clusters[group[j]].bounds);
        }

        var merged = MeshOptApi.ComputeSphereBounds(
            (float*)boundsList.GetUnsafePtr(),
            (nuint)group.Count,
            (nuint)sizeof(ClodBounds),
            (float*)boundsList.GetUnsafePtr() + 3,
            (nuint)sizeof(ClodBounds)
        );

        var maxError = 0.0f;
        for (var j = 0; j < group.Count; j++)
        {
            maxError = Math.Max(maxError, clusters[group[j]].bounds.error);
        }

        return new ClodBounds
        {
            center = new float3(merged.center[0], merged.center[1], merged.center[2]),
            radius = merged.radius,
            error = maxError
        };
    }

    private static UnsafeList<Cluster> Clusterize(ClodConfig config, ClodMesh mesh, uint* indices, nuint indexCount, Allocator allocator)
    {
        var maxMeshlets = MeshOptApi.BuildMeshletsBound(indexCount, config.maxVertices, config.minTriangles);

        using var meshlets = new UnsafeArray<meshopt_Meshlet>((int)maxMeshlets, Allocator.FreeList);
        using var meshletVertices = new UnsafeArray<uint>((int)indexCount, Allocator.FreeList);
        using var meshletTriangles = new UnsafeArray<byte>((int)indexCount, Allocator.FreeList);

        var pMeshlets = (meshopt_Meshlet*)meshlets.GetUnsafePtr();
        var pMeshletVertices = (uint*)meshletVertices.GetUnsafePtr();
        var pMeshletTriangles = (byte*)meshletTriangles.GetUnsafePtr();

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
                indices = new UnsafeList<uint>((int)(meshlet.triangle_count * 3), Allocator.Persistent),
                group = -1,
                refined = -1
            };

            for (nuint j = 0; j < meshlet.triangle_count * 3; j++)
            {
                cluster.indices.Add(pMeshletVertices[meshlet.vertex_offset + pMeshletTriangles[meshlet.triangle_offset + j]]);
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    internal static void LockBoundary(UnsafeArray<byte> locks, UnsafeList<UnsafeList<int>> groups, UnsafeList<Cluster> clusters, UnsafeArray<uint> remap, byte* vertexLock)
    {
        var pLocks = (byte*)locks.GetUnsafePtr();
        var pRemap = (uint*)remap.GetUnsafePtr();

        for (var i = 0; i < locks.Length; i++)
        {
            pLocks[i] = unchecked((byte)(pLocks[i] & ~((1 << 0) | (1 << 7))));
        }

        for (var i = 0; i < groups.Count; i++)
        {
            for (var j = 0; j < groups[i].Count; j++)
            {
                var cluster = clusters[groups[i][j]];
                for (var k = 0; k < cluster.indices.Count; k++)
                {
                    var r = pRemap[(int)cluster.indices[k]];
                    pLocks[r] |= (byte)(pLocks[r] >> 7);
                }
            }

            for (var j = 0; j < groups[i].Count; j++)
            {
                var cluster = clusters[groups[i][j]];
                for (var k = 0; k < cluster.indices.Count; k++)
                {
                    var r = pRemap[(int)cluster.indices[k]];
                    pLocks[r] |= 1 << 7;
                }
            }
        }

        for (var i = 0; i < locks.Length; i++)
        {
            var r = pRemap[i];
            pLocks[i] = (byte)((pLocks[r] & 1) | (pLocks[i] & (byte)SimplifyVertexOptions.Protect & 0xFF));
            if (vertexLock != null)
            {
                pLocks[i] |= vertexLock[i];
            }
        }
    }

    private static UnsafeList<UnsafeList<int>> Partition(ClodConfig config, ClodMesh mesh, UnsafeList<Cluster> clusters, UnsafeList<int> pending, UnsafeArray<uint> remap, Allocator allocator)
    {
        if (pending.Count <= (int)config.partitionSize)
        {
            var single = new UnsafeList<UnsafeList<int>>(1, allocator);
            single.Add(pending);
            return single;
        }

        nuint totalIndexCount = 0;
        for (var i = 0; i < pending.Count; i++)
        {
            totalIndexCount += (nuint)clusters[pending[i]].indices.Count;
        }

        using var clusterIndices = new UnsafeList<uint>((int)totalIndexCount, Allocator.FreeList);
        using var clusterCounts = new UnsafeList<uint>(pending.Count, Allocator.FreeList);

        nuint offset = 0;
        for (var i = 0; i < pending.Count; i++)
        {
            var cluster = clusters[pending[i]];
            clusterCounts.Add((uint)cluster.indices.Count);
            for (var j = 0; j < cluster.indices.Count; j++)
            {
                clusterIndices.Add(((uint*)remap.GetUnsafePtr())[(int)cluster.indices[j]]);
            }

            offset += (nuint)cluster.indices.Count;
        }

        using var clusterPart = new UnsafeArray<uint>(pending.Count, Allocator.FreeList);

        var partitionCount = MeshOptApi.PartitionClusters(
            (uint*)clusterPart.GetUnsafePtr(),
            (uint*)clusterIndices.GetUnsafePtr(),
            totalIndexCount,
            (uint*)clusterCounts.GetUnsafePtr(),
            (nuint)pending.Count,
            config.partitionSpatial ? mesh.vertexPositions : null,
            (nuint)remap.Length,
            mesh.vertexPositionsStride,
            config.partitionSize
        );

        var partitions = new UnsafeList<UnsafeList<int>>((int)partitionCount, allocator);
        for (nuint i = 0; i < partitionCount; i++)
        {
            partitions.Add(new UnsafeList<int>((int)(config.partitionSize + config.partitionSize / 3), allocator));
        }

        for (var i = 0; i < pending.Count; i++)
        {
            partitions[(int)((uint*)clusterPart.GetUnsafePtr())[i]].Add(pending[i]);
        }

        return partitions;
    }

    private static int OutputGroup(ClodConfig config, ClodMesh mesh, UnsafeList<Cluster> clusters, UnsafeList<int> group, ClodBounds simplified, int depth, void* outputContext, ClodOutputDelegate? outputCallback)
    {
        using var groupClusters = new UnsafeList<ClodCluster>(group.Count, Allocator.FreeList);

        for (var i = 0; i < group.Count; i++)
        {
            ref var srcCluster = ref clusters[group[i]];
            groupClusters.Add(new ClodCluster
            {
                refined = srcCluster.refined,
                bounds = (config.optimizeBounds && srcCluster.refined != -1)
                    ? ComputeBounds(mesh, srcCluster.indices, srcCluster.bounds.error)
                    : srcCluster.bounds,
                indices = (uint*)srcCluster.indices.GetUnsafePtr(),
                indexCount = (nuint)srcCluster.indices.Count,
                vertexCount = srcCluster.vertices
            });
        }

        var clodGroup = new ClodGroup { depth = depth, simplified = simplified };
        var result = outputCallback != null
            ? outputCallback(outputContext, clodGroup, groupClusters.AsReadOnly())
            : -1;

        return result;
    }

    public static UnsafeArray<uint> Simplify(ClodConfig config, ClodMesh mesh, ReadOnlyUnsafeCollection<uint> indices, ReadOnlyUnsafeCollection<byte> locks, nuint targetCount, float* error, Allocator allocator)
    {
        var lod = new UnsafeArray<uint>(indices.Count, allocator);

        if (targetCount >= (nuint)indices.Count)
        {
            lod.CopyFrom(indices.AsSpan());
            return lod;
        }

        var options = SimplifyOptions.Sparse | SimplifyOptions.ErrorAbsolute;
        if (config.simplifyPermissive)
        {
            options |= SimplifyOptions.Permissive;
        }

        if (config.simplifyRegularize)
        {
            options |= SimplifyOptions.Regularize;
        }

        var resultSize = MeshOptApi.SimplifyWithAttributes(
            (uint*)lod.GetUnsafePtr(),
            (uint*)indices.GetUnsafePtr(),
            (nuint)indices.Count,
            mesh.vertexPositions,
            mesh.vertexCount,
            mesh.vertexPositionsStride,
            mesh.vertexAttributes,
            mesh.vertexAttributesStride,
            mesh.attributeWeights,
            mesh.attributeCount,
            (byte*)locks.GetUnsafePtr(),
            targetCount,
            float.MaxValue,
            options,
            error
        );

        lod.Resize((int)resultSize);

        if ((nuint)lod.Length > targetCount && config.simplifyFallbackPermissive && !config.simplifyPermissive)
        {
            options |= SimplifyOptions.Permissive;
            resultSize = MeshOptApi.SimplifyWithAttributes(
                (uint*)lod.GetUnsafePtr(),
                (uint*)indices.GetUnsafePtr(),
                (nuint)indices.Count,
                mesh.vertexPositions,
                mesh.vertexCount,
                mesh.vertexPositionsStride,
                mesh.vertexAttributes,
                mesh.vertexAttributesStride,
                mesh.attributeWeights,
                mesh.attributeCount,
                (byte*)locks.GetUnsafePtr(),
                targetCount,
                float.MaxValue,
                options,
                error
            );

            lod.Resize((int)resultSize);
        }

        if ((nuint)lod.Length > targetCount && config.simplifyFallbackSloppy)
        {
            *error *= config.simplifyErrorFactorSloppy;
        }

        if (config.simplifyErrorEdgeLimit > 0)
        {
            float maxEdgeSq = 0;
            var pIdx = (uint*)indices.GetUnsafePtr();
            var posStride = mesh.vertexPositionsStride / (nuint)sizeof(float);

            for (var i = 0; i < indices.Count; i += 3)
            {
                uint a = pIdx[i], b = pIdx[i + 1], c = pIdx[i + 2];
                var va = mesh.vertexPositions + (a * posStride);
                var vb = mesh.vertexPositions + (b * posStride);
                var vc = mesh.vertexPositions + (c * posStride);

                float dx, dy, dz;
                dx = va[0] - vb[0]; dy = va[1] - vb[1]; dz = va[2] - vb[2];
                var eab = dx * dx + dy * dy + dz * dz;
                dx = va[0] - vc[0]; dy = va[1] - vc[1]; dz = va[2] - vc[2];
                var eac = dx * dx + dy * dy + dz * dz;
                dx = vb[0] - vc[0]; dy = vb[1] - vc[1]; dz = vb[2] - vc[2];
                var ebc = dx * dx + dy * dy + dz * dz;

                var emax = Math.Max(Math.Max(eab, eac), ebc);
                var emin = Math.Min(Math.Min(eab, eac), ebc);
                maxEdgeSq = Math.Max(maxEdgeSq, Math.Max(emin, emax / 4));
            }

            *error = Math.Min(*error, (float)Math.Sqrt(maxEdgeSq) * config.simplifyErrorEdgeLimit);
        }

        return lod;
    }

    /// <summary>
    /// Builds a cluster LOD hierarchy from the input mesh.
    /// </summary>
    /// <param name="config">The configuration parameters for the LOD building process.</param>
    /// <param name="mesh">The input mesh data.</param>
    /// <param name="outputContext">Optional context pointer passed to the output callback.</param>
    /// <param name="outputCallback">Delegate invoked for each generated LOD group.</param>
    /// <returns>The total count of generated clusters.</returns>
    public static nuint Build(ClodConfig config, ClodMesh mesh, void* outputContext, ClodOutputDelegate? outputCallback)
    {
        Debug.Assert(mesh.vertexAttributesStride % sizeof(float) == 0, "vertexAttributesStride must be a multiple of sizeof(float)");

        using var locks = new UnsafeArray<byte>((int)mesh.vertexCount, Allocator.FreeList, AllocationOption.Clear);
        using var remap = new UnsafeArray<uint>((int)mesh.vertexCount, Allocator.FreeList);

        MeshOptApi.GeneratePositionRemap((uint*)remap.GetUnsafePtr(), mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);

        if (mesh.attributeProtectMask != 0)
        {
            var maxAttributes = mesh.vertexAttributesStride / sizeof(float);
            for (nuint i = 0; i < mesh.vertexCount; i++)
            {
                var r = ((uint*)remap.GetUnsafePtr())[(int)i];
                for (nuint j = 0; j < maxAttributes; j++)
                {
                    if ((r != i) && ((mesh.attributeProtectMask & (1u << (int)j)) != 0))
                    {
                        if (mesh.vertexAttributes[i * maxAttributes + j] != mesh.vertexAttributes[r * maxAttributes + j])
                        {
                            ((byte*)locks.GetUnsafePtr())[i] |= (byte)SimplifyVertexOptions.Protect & 0xFF;
                        }
                    }
                }
            }
        }

        using var clusters = Clusterize(config, mesh, mesh.indices, mesh.indexCount, Allocator.FreeList);

        for (var i = 0; i < clusters.Count; i++)
        {
            clusters[i].bounds = ComputeBounds(mesh, clusters[i].indices, 0.0f);
        }

        using var pending = new UnsafeList<int>(clusters.Count, Allocator.FreeList);
        for (var i = 0; i < clusters.Count; i++)
        {
            pending.Add(i);
        }

        var depth = 0;

        while (pending.Count > 1)
        {
            using var groups = Partition(config, mesh, clusters, pending, remap, Allocator.FreeList);
            pending.Clear();

            LockBoundary(locks, groups, clusters, remap, mesh.vertexLock);

            for (var i = 0; i < groups.Count; i++)
            {
                using var merged = new UnsafeList<uint>(groups[i].Count * (int)config.maxTriangles * 3, Allocator.FreeList);
                for (var j = 0; j < groups[i].Count; j++)
                {
                    var clusterIndices = clusters[groups[i][j]].indices;
                    for (var k = 0; k < clusterIndices.Count; k++)
                    {
                        merged.Add(clusterIndices[k]);
                    }
                }

                var targetSize = ((nuint)merged.Count / 3) * (nuint)config.simplifyRatio * 3;
                var bounds = MergeBounds(clusters, groups[i]);

                var error = 0.0f;
                using var simplified = Simplify(config, mesh, merged.AsReadOnly(), locks.AsReadOnly(), targetSize, &error, Allocator.FreeList);

                if ((nuint)simplified.Length > (nuint)(merged.Count * config.simplifyThreshold))
                {
                    bounds.error = float.MaxValue;
                    OutputGroup(config, mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback);
                    continue;
                }

                bounds.error = Math.Max(bounds.error * config.simplifyErrorMergePrevious, error) + error * config.simplifyErrorMergeAdditive;

                var refined = OutputGroup(config, mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback);

                for (var j = 0; j < groups[i].Count; j++)
                {
                    clusters[groups[i][j]].Dispose();
                }

                using var split = Clusterize(config, mesh, (uint*)simplified.GetUnsafePtr(), (nuint)simplified.Length, Allocator.FreeList);
                for (var j = 0; j < split.Count; j++)
                {
                    split[j].refined = refined;
                    split[j].bounds = bounds;
                    clusters.Add(split[j]);
                    pending.Add(clusters.Count - 1);
                }
            }

            for (var i = 0; i < groups.Count; i++)
            {
                groups[i].Dispose();
            }

            depth++;
        }

        if (pending.Count > 0)
        {
            var bounds = clusters[pending[0]].bounds;
            bounds.error = float.MaxValue;
            OutputGroup(config, mesh, clusters, pending, bounds, depth, outputContext, outputCallback);
        }

        var finalClusterCount = (nuint)clusters.Count;

        for (var i = 0; i < clusters.Count; i++)
        {
            clusters[i].Dispose();
        }

        return finalClusterCount;
    }
}