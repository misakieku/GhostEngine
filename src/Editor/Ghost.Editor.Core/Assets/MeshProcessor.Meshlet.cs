// Source: https://github.com/zeux/meshoptimizer/blob/master/demo/clusterlod.h
// Translated from C++ to C#.

// TODO: This file should be moved to editor project since there is no reason we need to build meshlets and LOD at runtime.

using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;

namespace Ghost.Editor.Core.Assets;

internal struct Cluster : IDisposable
{
    public UnsafeList<uint> indices;
    public UnsafeList<uint> uniqueVertices;
    public UnsafeList<byte> localIndices;
    public ClodBounds bounds;
    public nuint vertices;
    public int group;
    public int refined;

    public void Dispose()
    {
        indices.Dispose();
        uniqueVertices.Dispose();
        localIndices.Dispose();
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
    /// <summary> Level of cluster optimization. </summary>
    public int optimizeClustersLevel;
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
    /// <summary> Pointer to unique vertices for this cluster. </summary>
    public uint* uniqueVertices;
    /// <summary> Number of unique vertices in the cluster. </summary>
    public nuint vertexCount;
    /// <summary> Pointer to local triangle indices for this cluster. </summary>
    public byte* localIndices;
    /// <summary> Number of local indices. </summary>
    public nuint localIndexCount;
}

/// <summary>
/// Delegate type for processing generated LOD groups.
/// </summary>
public unsafe delegate int ClodOutputDelegate(void* context, ClodGroup group, ReadOnlyUnsafeCollection<ClodCluster> clusters);

// FIX: UnsafeList and UnsafeArray are not same as std::vector.

public static unsafe partial class MeshProcessor
{
    private static ClodBounds ComputeBounds(ref readonly ClodMesh mesh, UnsafeList<uint> indices, float error)
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
        using var boundsList = new UnsafeArray<ClodBounds>(group.Count, AllocationHandle.FreeList);
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

    private static UnsafeList<Cluster> Clusterize(ref readonly ClodConfig config, ref readonly ClodMesh mesh, uint* indices, nuint indexCount)
    {
        var maxMeshlets = MeshOptApi.BuildMeshletsBound(indexCount, config.maxVertices, config.minTriangles);

        using var meshlets = new UnsafeArray<meshopt_Meshlet>((int)maxMeshlets, AllocationHandle.FreeList);
        using var meshletVertices = new UnsafeArray<uint>((int)indexCount, AllocationHandle.FreeList);
        using var meshletTriangles = new UnsafeArray<byte>((int)indexCount, AllocationHandle.FreeList);

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

        var clusters = new UnsafeList<Cluster>((int)meshletCount, AllocationHandle.FreeList);

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
                indices = new UnsafeList<uint>((int)(meshlet.triangle_count * 3), AllocationHandle.FreeList),
                uniqueVertices = new UnsafeList<uint>((int)meshlet.vertex_count, AllocationHandle.FreeList),
                localIndices = new UnsafeList<byte>((int)(meshlet.triangle_count * 3), AllocationHandle.FreeList),
                group = -1,
                refined = -1
            };

            for (nuint j = 0; j < meshlet.vertex_count; j++)
            {
                cluster.uniqueVertices.Add(pMeshletVertices[meshlet.vertex_offset + j]);
            }

            for (nuint j = 0; j < meshlet.triangle_count * 3; j++)
            {
                var localIdx = pMeshletTriangles[meshlet.triangle_offset + j];
                cluster.localIndices.Add(localIdx);
                cluster.indices.Add(pMeshletVertices[meshlet.vertex_offset + localIdx]);
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

    private static UnsafeList<UnsafeList<int>> Partition(ref readonly ClodConfig config, ref readonly ClodMesh mesh, UnsafeList<Cluster> clusters, UnsafeList<int> pending, UnsafeArray<uint> remap)
    {
        if (pending.Count <= (int)config.partitionSize)
        {
            var single = new UnsafeList<UnsafeList<int>>(1, AllocationHandle.FreeList);
            var pendingcpy = new UnsafeList<int>(pending.Count, AllocationHandle.FreeList);

            pendingcpy.AddRange(pending.AsSpan());
            single.Add(pendingcpy);

            return single;
        }

        nuint totalIndexCount = 0;
        for (var i = 0; i < pending.Count; i++)
        {
            totalIndexCount += (nuint)clusters[pending[i]].indices.Count;
        }

        using var clusterIndices = new UnsafeList<uint>((int)totalIndexCount, AllocationHandle.FreeList);
        using var clusterCounts = new UnsafeList<uint>(pending.Count, AllocationHandle.FreeList);

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

        using var clusterPart = new UnsafeArray<uint>(pending.Count, AllocationHandle.FreeList);

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

        var partitions = new UnsafeList<UnsafeList<int>>((int)partitionCount, AllocationHandle.FreeList);
        for (nuint i = 0; i < partitionCount; i++)
        {
            partitions.Add(new UnsafeList<int>((int)(config.partitionSize + config.partitionSize / 3), AllocationHandle.FreeList));
        }

        for (var i = 0; i < pending.Count; i++)
        {
            partitions[(int)clusterPart[i]].Add(pending[i]);
        }

        return partitions;
    }

    private static int OutputGroup(ref readonly ClodConfig config, ref readonly ClodMesh mesh, UnsafeList<Cluster> clusters, UnsafeList<int> group, ClodBounds simplified, int depth, void* outputContext, ClodOutputDelegate? outputCallback)
    {
        using var groupClusters = new UnsafeList<ClodCluster>(group.Count, AllocationHandle.FreeList);

        for (var i = 0; i < group.Count; i++)
        {
            ref var srcCluster = ref clusters[group[i]];
            groupClusters.Add(new ClodCluster
            {
                refined = srcCluster.refined,
                bounds = (config.optimizeBounds && srcCluster.refined != -1)
                    ? ComputeBounds(in mesh, srcCluster.indices, srcCluster.bounds.error)
                    : srcCluster.bounds,
                indices = (uint*)srcCluster.indices.GetUnsafePtr(),
                indexCount = (nuint)srcCluster.indices.Count,
                uniqueVertices = (uint*)srcCluster.uniqueVertices.GetUnsafePtr(),
                vertexCount = srcCluster.vertices,
                localIndices = (byte*)srcCluster.localIndices.GetUnsafePtr(),
                localIndexCount = (nuint)srcCluster.localIndices.Count
            });
        }

        var clodGroup = new ClodGroup { depth = depth, simplified = simplified };
        var result = outputCallback != null
            ? outputCallback(outputContext, clodGroup, groupClusters.AsReadOnly())
            : -1;

        return result;
    }

    private struct SloppyVertex
    {
        public float x, y, z;
        public uint id;
    }

    private static void SimplifyFallback(ref UnsafeArray<uint> lod, ref readonly ClodMesh mesh, ReadOnlyUnsafeCollection<uint> indices, ReadOnlyUnsafeCollection<byte> locks, nuint target_count, float* error)
    {
        using var subset = new UnsafeArray<SloppyVertex>(indices.Count, AllocationHandle.FreeList);
        using var subset_locks = new UnsafeArray<byte>(indices.Count, AllocationHandle.FreeList);

        lod.Resize(indices.Count);

        var positions_stride = mesh.vertexPositionsStride / sizeof(float);

        // deindex the mesh subset to avoid calling simplifySloppy on the entire vertex buffer (which is prohibitively expensive without sparsity)
        for (var i = 0; i < indices.Count; ++i)
        {
            var v = indices[i];
            Logger.DebugAssert(v < mesh.vertexCount);

            subset[i].x = mesh.vertexPositions[v * positions_stride + 0];
            subset[i].y = mesh.vertexPositions[v * positions_stride + 1];
            subset[i].z = mesh.vertexPositions[v * positions_stride + 2];
            subset[i].id = v;

            subset_locks[i] = locks[v];
            lod[i] = (uint)i;
        }

        var newSize = MeshOptApi.SimplifySloppy((uint*)lod.GetUnsafePtr(), (uint*)lod.GetUnsafePtr(), (nuint)lod.Count, (float*)subset.GetUnsafePtr(), (nuint)subset.Count, (nuint)sizeof(SloppyVertex), (byte*)subset_locks.GetUnsafePtr(), target_count, float.MaxValue, error);
        lod.Resize((int)newSize);

        // convert error to absolute
        *error *= MeshOptApi.SimplifyScale((float*)subset.GetUnsafePtr(), (nuint)subset.Count, (nuint)sizeof(SloppyVertex));

        // restore original vertex indices
        for (var i = 0; i < lod.Count; ++i)
        {
            lod[i] = subset[lod[i]].id;
        }
    }

    public static UnsafeArray<uint> Simplify(ref readonly ClodConfig config, ref readonly ClodMesh mesh, ReadOnlyUnsafeCollection<uint> indices, ReadOnlyUnsafeCollection<byte> locks, nuint targetCount, float* error)
    {
        var lod = new UnsafeArray<uint>(indices.Count, AllocationHandle.FreeList);

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
            SimplifyFallback(ref lod, in mesh, indices, locks, targetCount, error);
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
    public static nuint Build(ref readonly ClodConfig config, ref readonly ClodMesh mesh, void* outputContext, ClodOutputDelegate? outputCallback)
    {
        Logger.DebugAssert(mesh.vertexAttributesStride % sizeof(float) == 0, "vertexAttributesStride must be a multiple of sizeof(float)");

        using var locks = new UnsafeArray<byte>((int)mesh.vertexCount, AllocationHandle.FreeList, AllocationOption.Clear); ;
        using var remap = new UnsafeArray<uint>((int)mesh.vertexCount, AllocationHandle.FreeList);

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

        using var clusters = Clusterize(in config, in mesh, mesh.indices, mesh.indexCount);

        for (var i = 0; i < clusters.Count; i++)
        {
            clusters[i].bounds = ComputeBounds(in mesh, clusters[i].indices, 0.0f);
        }

        using var pending = new UnsafeList<int>(clusters.Count, AllocationHandle.FreeList);
        for (var i = 0; i < clusters.Count; i++)
        {
            pending.Add(i);
        }

        var depth = 0;

        while (pending.Count > 1)
        {
            using var groups = Partition(in config, in mesh, clusters, pending, remap);
            pending.Clear();

            LockBoundary(locks, groups, clusters, remap, mesh.vertexLock);

            for (var i = 0; i < groups.Count; i++)
            {
                using var merged = new UnsafeList<uint>(groups[i].Count * (int)config.maxTriangles * 3, AllocationHandle.FreeList);
                for (var j = 0; j < groups[i].Count; j++)
                {
                    var clusterIndices = clusters[groups[i][j]].indices;
                    merged.AddRange(clusterIndices.AsSpan());
                }

                var targetSize = (nuint)(merged.Count / 3 * config.simplifyRatio * 3.0f);
                var bounds = MergeBounds(clusters, groups[i]);

                var error = 0.0f;
                using var simplified = Simplify(in config, in mesh, merged.AsReadOnly(), locks.AsReadOnly(), targetSize, &error);

                if ((nuint)simplified.Length > (nuint)(merged.Count * config.simplifyThreshold))
                {
                    bounds.error = float.MaxValue;
                    OutputGroup(in config, in mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback);
                    continue;
                }

                bounds.error = Math.Max(bounds.error * config.simplifyErrorMergePrevious, error) + error * config.simplifyErrorMergeAdditive;

                var refined = OutputGroup(in config, in mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback);

                for (var j = 0; j < groups[i].Count; j++)
                {
                    clusters[groups[i][j]].Dispose();
                }

                using var split = Clusterize(in config, in mesh, (uint*)simplified.GetUnsafePtr(), (nuint)simplified.Length);
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
            OutputGroup(in config, in mesh, clusters, pending, bounds, depth, outputContext, outputCallback);
        }

        var finalClusterCount = (nuint)clusters.Count;

        for (var i = 0; i < clusters.Count; i++)
        {
            clusters[i].Dispose();
        }

        return finalClusterCount;
    }

    private struct MeshletContext
    {
        public MeshletMeshData* data;
        public int materialIndex;
    }

    public static void BuildMeshlets(MeshletMeshData* pMeshletData, ReadOnlyUnsafeCollection<Vertex> vertices, ReadOnlyUnsafeCollection<uint> indices, int materialIndex = 0)
    {
        Logger.DebugAssert(pMeshletData->meshletCount > 0, "Mesh must have vertices to build meshlets.");

        var config = new ClodConfig
        {
            maxVertices = 64,
            minTriangles = 32,
            maxTriangles = 124,

            partitionSpatial = true,
            partitionSize = 16,

            clusterSpatial = false,
            clusterSplitFactor = 2.0f,

            optimizeClusters = true,
            optimizeClustersLevel = 1,

            simplifyRatio = 0.5f,
            simplifyThreshold = 0.85f,
            simplifyErrorMergePrevious = 1.0f,
            simplifyErrorFactorSloppy = 2.0f,
            simplifyPermissive = true,
            simplifyFallbackPermissive = false,
            simplifyFallbackSloppy = true,
        };

        var clodMesh = new ClodMesh
        {
            vertexPositions = (float*)Unsafe.AsPointer(in vertices[0].position),
            vertexCount = (nuint)vertices.Count,
            vertexPositionsStride = (nuint)sizeof(Vertex),
            vertexAttributes = (float*)Unsafe.AsPointer(in vertices[0].normal),
            vertexAttributesStride = (nuint)sizeof(Vertex),
            indices = (uint*)indices.GetUnsafePtr(),
            indexCount = (nuint)indices.Count,
            attributeProtectMask = 0, // TODO: We need to protect UVs and other vertex attributes to ensure they are not altered during simplification.
        };

        var context = new MeshletContext
        {
            data = pMeshletData,
            materialIndex = materialIndex
        };

        Build(in config, in clodMesh, &context, MeshletOutputCallback);

        pMeshletData->meshletCount = pMeshletData->meshlets.IsCreated ? pMeshletData->meshlets.Count : 0;

        if (pMeshletData->groups.IsCreated && pMeshletData->groups.Count > 0)
        {
            var maxLodLevel = 0u;
            for (var i = 0; i < pMeshletData->groups.Count; i++)
            {
                maxLodLevel = Math.Max(maxLodLevel, pMeshletData->groups[i].lodLevel);
            }

            pMeshletData->lodLevelCount = (int)maxLodLevel + 1;
        }

        pMeshletData->materialSlotCount = Math.Max(pMeshletData->materialSlotCount, materialIndex + 1);
    }

    private static int MeshletOutputCallback(void* contextPtr, ClodGroup group, ReadOnlyUnsafeCollection<ClodCluster> clusters)
    {
        var context = (MeshletContext*)contextPtr;
        var pMeshletData = context->data;
        var materialIndex = context->materialIndex;

        // Ensure lists are initialized
        if (!pMeshletData->groups.IsCreated) pMeshletData->groups = new UnsafeList<MeshletGroup>(16, AllocationHandle.Persistent);
        if (!pMeshletData->meshlets.IsCreated) pMeshletData->meshlets = new UnsafeList<Meshlet>(64, AllocationHandle.Persistent);
        if (!pMeshletData->meshletVertices.IsCreated) pMeshletData->meshletVertices = new UnsafeList<uint>(128, AllocationHandle.Persistent);
        if (!pMeshletData->meshletTriangles.IsCreated) pMeshletData->meshletTriangles = new UnsafeList<uint>(128, AllocationHandle.Persistent);

        var meshletGroup = new MeshletGroup
        {
            boundingSphere = new SphereBounds(group.simplified.center, group.simplified.radius),
            boundingBox = new AABB(group.simplified.center - group.simplified.radius, group.simplified.center + group.simplified.radius),
            parentError = group.simplified.error,
            meshletStartIndex = (uint)pMeshletData->meshlets.Count,
            meshletCount = (uint)clusters.Count,
            lodLevel = (uint)group.depth
        };
        pMeshletData->groups.Add(meshletGroup);

        for (var i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];

            var meshlet = new Meshlet
            {
                boundingSphere = new SphereBounds(cluster.bounds.center, cluster.bounds.radius),
                parentBoundingSphere = new SphereBounds(group.simplified.center, group.simplified.radius),
                boundingBox = new AABB(cluster.bounds.center - cluster.bounds.radius, cluster.bounds.center + cluster.bounds.radius),
                vertexCount = (byte)cluster.vertexCount,
                triangleCount = (byte)(cluster.localIndexCount / 3),
                vertexOffset = (uint)pMeshletData->meshletVertices.Count,
                triangleOffset = (uint)pMeshletData->meshletTriangles.Count,
                groupIndex = (uint)pMeshletData->groups.Count - 1,
                clusterError = cluster.bounds.error,
                parentError = group.simplified.error,
                localMaterialIndex = (byte)materialIndex,
                lodLevel = (byte)group.depth,
            };
            pMeshletData->meshlets.Add(meshlet);

            // Add unique vertices
            for (nuint j = 0; j < cluster.vertexCount; j++)
            {
                pMeshletData->meshletVertices.Add(cluster.uniqueVertices[j]);
            }
            // Add local triangles (packed into uints)
            var triangleCount = cluster.localIndexCount / 3;
            for (nuint j = 0; j < triangleCount; j++)
            {
                uint i0 = cluster.localIndices[j * 3 + 0];
                uint i1 = cluster.localIndices[j * 3 + 1];
                uint i2 = cluster.localIndices[j * 3 + 2];
                var packedTriangle = i0 | (i1 << 8) | (i2 << 16);
                pMeshletData->meshletTriangles.Add(packedTriangle);
            }
        }

        return 0;
    }

    public static void BuildClusterLodHierarchy()
    {
        // TODO: Implement a function that builds a cluster LOD hierarchy for a mesh, which can be used for efficient rendering of large meshes with varying levels of detail.
    }
}
