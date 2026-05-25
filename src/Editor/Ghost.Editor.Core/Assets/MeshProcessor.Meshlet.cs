// Source: https://github.com/zeux/meshoptimizer/blob/master/demo/clusterlod.h
// Translated from C++ to C#.

using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

internal static unsafe partial class MeshProcessor
{
    private delegate int ClodOutputDelegate(MeshletContext context, ClodGroup group, ReadOnlyView<ClodCluster> clusters);

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

    private static ClodBounds MergeBounds(UnsafeList<Cluster> clusters, UnsafeList<int> group, AllocationHandle allocationHandle)
    {
        using var boundsList = new UnsafeArray<ClodBounds>(group.Count, allocationHandle);
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

    private static UnsafeList<Cluster> Clusterize(ref readonly ClodConfig config, ref readonly ClodMesh mesh, uint* indices, nuint indexCount, AllocationHandle allocationHandle)
    {
        var maxMeshlets = MeshOptApi.BuildMeshletsBound(indexCount, config.maxVertices, config.minTriangles);

        using var meshlets = new UnsafeArray<meshopt_Meshlet>((int)maxMeshlets, allocationHandle);
        using var meshletVertices = new UnsafeArray<uint>((int)indexCount, allocationHandle);
        using var meshletTriangles = new UnsafeArray<byte>((int)indexCount, allocationHandle);

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

        var clusters = new UnsafeList<Cluster>((int)meshletCount, allocationHandle);

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
                indices = new UnsafeList<uint>((int)(meshlet.triangle_count * 3), allocationHandle),
                uniqueVertices = new UnsafeList<uint>((int)meshlet.vertex_count, allocationHandle),
                localIndices = new UnsafeList<byte>((int)(meshlet.triangle_count * 3), allocationHandle),
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

    private static UnsafeList<UnsafeList<int>> Partition(ref readonly ClodConfig config, ref readonly ClodMesh mesh, UnsafeList<Cluster> clusters, UnsafeList<int> pending, UnsafeArray<uint> remap, AllocationHandle allocationHandle)
    {
        if (pending.Count <= (int)config.partitionSize)
        {
            var single = new UnsafeList<UnsafeList<int>>(1, allocationHandle);
            var pendingcpy = new UnsafeList<int>(pending.Count, allocationHandle);

            pendingcpy.AddRange(pending.AsSpan());
            single.Add(pendingcpy);

            return single;
        }

        nuint totalIndexCount = 0;
        for (var i = 0; i < pending.Count; i++)
        {
            totalIndexCount += (nuint)clusters[pending[i]].indices.Count;
        }

        using var clusterIndices = new UnsafeList<uint>((int)totalIndexCount, allocationHandle);
        using var clusterCounts = new UnsafeList<uint>(pending.Count, allocationHandle);

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

        using var clusterPart = new UnsafeArray<uint>(pending.Count, allocationHandle);

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

        var partitions = new UnsafeList<UnsafeList<int>>((int)partitionCount, allocationHandle);
        for (nuint i = 0; i < partitionCount; i++)
        {
            partitions.Add(new UnsafeList<int>((int)(config.partitionSize + config.partitionSize / 3), allocationHandle));
        }

        for (var i = 0; i < pending.Count; i++)
        {
            partitions[(int)clusterPart[i]].Add(pending[i]);
        }

        return partitions;
    }

    private static int OutputGroup(ref readonly ClodConfig config, ref readonly ClodMesh mesh,
        UnsafeList<Cluster> clusters, UnsafeList<int> group, ClodBounds simplified, int depth,
        MeshletContext outputContext, ClodOutputDelegate? outputCallback,
        AllocationHandle allocationHandle)
    {
        using var groupClusters = new UnsafeList<ClodCluster>(group.Count, allocationHandle);

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

    private static void SimplifyFallback(ref UnsafeArray<uint> lod, ref readonly ClodMesh mesh, ReadOnlyView<uint> indices, ReadOnlyView<byte> locks, nuint target_count, float* error, AllocationHandle allocationHandle)
    {
        using var subset = new UnsafeArray<SloppyVertex>(indices.Count, allocationHandle);
        using var subset_locks = new UnsafeArray<byte>(indices.Count, allocationHandle);

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

    private static UnsafeArray<uint> Simplify(ref readonly ClodConfig config, ref readonly ClodMesh mesh,
        ReadOnlyView<uint> indices, ReadOnlyView<byte> locks, nuint targetCount, float* error,
        AllocationHandle allocationHandle)
    {
        var lod = new UnsafeArray<uint>(indices.Count, allocationHandle);

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
            SimplifyFallback(ref lod, in mesh, indices, locks, targetCount, error, allocationHandle);
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
    private static nuint Build(ref readonly ClodConfig config, ref readonly ClodMesh mesh, MeshletContext outputContext, ClodOutputDelegate? outputCallback)
    {
        Logger.DebugAssert(mesh.vertexAttributesStride % sizeof(float) == 0, "vertexAttributesStride must be a multiple of sizeof(float)");

        using var locks = new UnsafeArray<byte>((int)mesh.vertexCount, AllocationHandle.TLSF, AllocationOption.Clear); ;
        using var remap = new UnsafeArray<uint>((int)mesh.vertexCount, AllocationHandle.TLSF);

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

        using var clusters = Clusterize(in config, in mesh, mesh.indices, mesh.indexCount, AllocationHandle.TLSF);

        for (var i = 0; i < clusters.Count; i++)
        {
            clusters[i].bounds = ComputeBounds(in mesh, clusters[i].indices, 0.0f);
        }

        using var pending = new UnsafeList<int>(clusters.Count, AllocationHandle.TLSF);
        for (var i = 0; i < clusters.Count; i++)
        {
            pending.Add(i);
        }

        var depth = 0;

        while (pending.Count > 1)
        {
            using var groups = Partition(in config, in mesh, clusters, pending, remap, AllocationHandle.TLSF);
            pending.Clear();

            LockBoundary(locks, groups, clusters, remap, mesh.vertexLock);

            for (var i = 0; i < groups.Count; i++)
            {
                using var merged = new UnsafeList<uint>(groups[i].Count * (int)config.maxTriangles * 3, AllocationHandle.TLSF);
                for (var j = 0; j < groups[i].Count; j++)
                {
                    var clusterIndices = clusters[groups[i][j]].indices;
                    merged.AddRange(clusterIndices.AsSpan());
                }

                var targetSize = (nuint)(merged.Count / 3 * config.simplifyRatio * 3.0f);
                var bounds = MergeBounds(clusters, groups[i], AllocationHandle.TLSF);

                var error = 0.0f;
                using var simplified = Simplify(in config, in mesh, merged.AsReadOnly(), locks.AsReadOnly(), targetSize, &error, AllocationHandle.TLSF);

                if ((nuint)simplified.Length > (nuint)(merged.Count * config.simplifyThreshold))
                {
                    bounds.error = float.MaxValue;
                    OutputGroup(in config, in mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback, AllocationHandle.TLSF);
                    continue;
                }

                bounds.error = Math.Max(bounds.error * config.simplifyErrorMergePrevious, error) + error * config.simplifyErrorMergeAdditive;

                var refined = OutputGroup(in config, in mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback, AllocationHandle.TLSF);

                for (var j = 0; j < groups[i].Count; j++)
                {
                    clusters[groups[i][j]].Dispose();
                }

                using var split = Clusterize(in config, in mesh, (uint*)simplified.GetUnsafePtr(), (nuint)simplified.Length, AllocationHandle.TLSF);
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
            OutputGroup(in config, in mesh, clusters, pending, bounds, depth, outputContext, outputCallback, AllocationHandle.TLSF);
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

    private static int MeshletOutputCallback(MeshletContext context, ClodGroup group, ReadOnlyView<ClodCluster> clusters)
    {
        var meshletData = context.data;
        var materialIndex = context.materialIndex;

        // Ensure lists are initialized
        if (!meshletData->groups.IsCreated)
        {
            meshletData->groups = new UnsafeList<MeshletGroup>(16, AllocationHandle.TLSF);
        }

        if (!meshletData->meshlets.IsCreated)
        {
            meshletData->meshlets = new UnsafeList<Meshlet>(64, AllocationHandle.TLSF);
        }

        if (!meshletData->meshletVertices.IsCreated)
        {
            meshletData->meshletVertices = new UnsafeList<uint>(128, AllocationHandle.TLSF);
        }

        if (!meshletData->meshletTriangles.IsCreated)
        {
            meshletData->meshletTriangles = new UnsafeList<uint>(128, AllocationHandle.TLSF);
        }

        var meshletGroup = new MeshletGroup
        {
            boundingSphere = new SphereBounds(group.simplified.center, group.simplified.radius),
            boundingBox = new AABB(group.simplified.center - group.simplified.radius, group.simplified.center + group.simplified.radius),
            parentError = group.simplified.error,
            meshletStartIndex = (uint)meshletData->meshlets.Count,
            meshletCount = (uint)clusters.Count,
            lodLevel = (uint)group.depth
        };
        meshletData->groups.Add(meshletGroup);

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
                vertexOffset = (uint)meshletData->meshletVertices.Count,
                triangleOffset = (uint)meshletData->meshletTriangles.Count,
                groupIndex = (uint)meshletData->groups.Count - 1,
                clusterError = cluster.bounds.error,
                parentError = group.simplified.error,
                localMaterialIndex = (byte)materialIndex,
                lodLevel = (byte)group.depth,
            };
            meshletData->meshlets.Add(meshlet);

            // Add unique vertices
            for (nuint j = 0; j < cluster.vertexCount; j++)
            {
                meshletData->meshletVertices.Add(cluster.uniqueVertices[j]);
            }
            // Add local triangles (packed into uints)
            var triangleCount = cluster.localIndexCount / 3;
            for (nuint j = 0; j < triangleCount; j++)
            {
                uint i0 = cluster.localIndices[j * 3 + 0];
                uint i1 = cluster.localIndices[j * 3 + 1];
                uint i2 = cluster.localIndices[j * 3 + 2];
                var packedTriangle = i0 | (i1 << 8) | (i2 << 16);
                meshletData->meshletTriangles.Add(packedTriangle);
            }
        }

        return meshletData->groups.Count - 1;
    }
}


internal static partial class MeshProcessor
{

    private struct MeshletBuildJob : IJob
    {
        public ClodConfig clodConfig;
        public ClodMesh clodMesh;

        public MeshletContext context;

        public readonly void Execute(ref readonly JobExecutionContext ctx)
        {
            Build(in clodConfig, in clodMesh, context, MeshletOutputCallback);
        }
    }

    /// <summary>
    /// Builds meshlets for a unified multi-material mesh.
    /// Each <see cref="MaterialPartInfo"/> describes a material partition's index range within the unified buffer.
    /// Meshlets are built per-part and tagged with the corresponding <c>localMaterialIndex</c>.
    /// </summary>
    public static async Task<DisposablePtr<MeshletMeshData>> BuildMeshletsAsync(JobScheduler jobScheduler,
        ReadOnlyView<Vertex> vertices, ReadOnlyView<uint> indices, ReadOnlyView<MaterialPartInfo> parts,
        CancellationToken token)
    {
        Logger.DebugAssert(vertices.Count > 0, "Mesh must have vertices to build meshlets.");
        Logger.DebugAssert(indices.Count > 0, "Mesh must have indices to build meshlets.");
        Logger.DebugAssert(parts.Length > 0, "Must have at least one material part.");

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

        var jobs = new MeshletBuildJob[parts.Length];

        IntPtr meshletData;
        unsafe
        {
            // NOTE: We use NativeMemory here instead of MemoryUtility (use mimalloc internally) because this is a async method and may run a random thread pool thread which never dies.
            // This will case mimalloc to allocate new heaps that hardly ever get freed, leading to memory bloat. Using NativeMemory ensures that we use the shared heap which doesn't have this issue.
            meshletData = (IntPtr)NativeMemory.AllocZeroed(MemoryUtility.SizeOf<MeshletMeshData>());
        }

        try
        {
            for (var i = 0; i < parts.Length; i++)
            {
                ref readonly var part = ref parts[i];

                unsafe
                {
                    // Each part references a slice of the global index buffer,
                    // but vertex positions are the full unified buffer so global indices remain valid.
                    var clodMesh = new ClodMesh
                    {
                        vertexPositions = (float*)Unsafe.AsPointer(in vertices[0].position),
                        vertexCount = (nuint)vertices.Count,
                        vertexPositionsStride = (nuint)sizeof(Vertex),
                        vertexAttributes = (float*)Unsafe.AsPointer(in vertices[0].normal),
                        vertexAttributesStride = (nuint)sizeof(Vertex),
                        indices = (uint*)indices.GetUnsafePtr() + part.indexStart,
                        indexCount = (nuint)part.indexCount,
                        attributeProtectMask = 0, // TODO: Protect UVs at material boundaries.
                    };

                    var context = new MeshletContext
                    {
                        data = (MeshletMeshData*)meshletData,
                        materialIndex = part.materialIndex
                    };

                    var job = new MeshletBuildJob
                    {
                        clodConfig = config,
                        clodMesh = clodMesh,
                        context = context
                    };

                    jobs[i] = job;
                }
            }

            foreach (var job in jobs)
            {
                var handle = jobScheduler.Schedule(in job);
                await jobScheduler.WaitAsync(handle, token);
            }

            unsafe
            {
                var pMeshletData = (MeshletMeshData*)meshletData;
                pMeshletData->meshletCount = pMeshletData->meshlets.IsCreated ? pMeshletData->meshlets.Count : 0;

                if (pMeshletData->groups.IsCreated && pMeshletData->groups.Count > 0)
                {
                    var maxLodLevel = 0u;
                    for (var j = 0; j < pMeshletData->groups.Count; j++)
                    {
                        maxLodLevel = Math.Max(maxLodLevel, pMeshletData->groups[j].lodLevel);
                    }

                    pMeshletData->lodLevelCount = (int)maxLodLevel + 1;
                }

                var maxMaterialSlot = 0;
                for (var j = 0; j < parts.Length; j++)
                {
                    maxMaterialSlot = Math.Max(maxMaterialSlot, parts[j].materialIndex);
                }

                pMeshletData->materialSlotCount = maxMaterialSlot + 1;

                return new DisposablePtr<MeshletMeshData>(pMeshletData);
            }
        }
        catch
        {
            unsafe
            {
                NativeMemory.Free((void*)meshletData);
            }

            throw;
        }
    }

    private struct TempBinaryNode
    {
        public AABB bounds;
        public float maxParentError;
        public int leftChild;
        public int rightChild;
        public int meshletIndex;
    }

    private static int BuildBinaryTree(ref UnsafeList<TempBinaryNode> nodes, UnsafeArray<int> meshletIndices, int start, int end, ReadOnlySpan<Meshlet> meshlets)
    {
        if (start == end - 1)
        {
            var meshletIndex = meshletIndices[start];
            ref readonly var m = ref meshlets[meshletIndex];

            var node = new TempBinaryNode
            {
                bounds = m.boundingBox,
                maxParentError = m.parentError,
                leftChild = -1,
                rightChild = -1,
                meshletIndex = meshletIndex
            };
            var nodeIndex = nodes.Count;
            nodes.Add(node);
            return nodeIndex;
        }

        // Compute centroid bounds
        var centroidMin = new float3(float.MaxValue);
        var centroidMax = new float3(float.MinValue);
        for (var i = start; i < end; i++)
        {
            var m = meshlets[meshletIndices[i]];
            var center = m.boundingBox.Center;
            centroidMin = math.min(centroidMin, center);
            centroidMax = math.max(centroidMax, center);
        }

        var extents = centroidMax - centroidMin;
        var splitAxis = 0;
        if (extents.y > extents.x && extents.y > extents.z)
        {
            splitAxis = 1;
        }

        if (extents.z > extents.x && extents.z > extents.y)
        {
            splitAxis = 2;
        }

        var splitPoint = centroidMin[splitAxis] + extents[splitAxis] * 0.5f;

        // Partition
        var mid = start;
        for (var i = start; i < end; i++)
        {
            var center = meshlets[meshletIndices[i]].boundingBox.Center;
            if (center[splitAxis] < splitPoint)
            {
                var temp = meshletIndices[mid];
                meshletIndices[mid] = meshletIndices[i];
                meshletIndices[i] = temp;
                mid++;
            }
        }

        if (mid == start || mid == end)
        {
            mid = start + (end - start) / 2;
        }

        var left = BuildBinaryTree(ref nodes, meshletIndices, start, mid, meshlets);
        var right = BuildBinaryTree(ref nodes, meshletIndices, mid, end, meshlets);

        var leftNode = nodes[left];
        var rightNode = nodes[right];

        var mergedBounds = new AABB(
            math.min(leftNode.bounds.Min, rightNode.bounds.Min),
            math.max(leftNode.bounds.Max, rightNode.bounds.Max)
        );

        var internalNodeIndex = nodes.Count;
        nodes.Add(new TempBinaryNode
        {
            bounds = mergedBounds,
            maxParentError = Math.Max(leftNode.maxParentError, rightNode.maxParentError),
            leftChild = left,
            rightChild = right,
            meshletIndex = -1
        });

        return internalNodeIndex;
    }

    private static void GatherChildren(UnsafeList<TempBinaryNode> binaryNodes, int nodeIndex, ref UnsafeList<int> gathered)
    {
        gathered.Clear();
        var node = binaryNodes[nodeIndex];
        if (node.leftChild != -1)
        {
            gathered.Add(node.leftChild);
        }

        if (node.rightChild != -1)
        {
            gathered.Add(node.rightChild);
        }

        while (gathered.Count < 4)
        {
            var largestInternalIndex = -1;
            var maxSurfaceArea = -1.0f;
            var listIndexToRemove = -1;

            for (var i = 0; i < gathered.Count; i++)
            {
                var childIdx = gathered[i];
                var childNode = binaryNodes[childIdx];
                if (childNode.leftChild != -1) // is internal
                {
                    var extents = childNode.bounds.Extents;
                    var sa = extents.x * extents.y + extents.y * extents.z + extents.z * extents.x;
                    if (sa > maxSurfaceArea)
                    {
                        maxSurfaceArea = sa;
                        largestInternalIndex = childIdx;
                        listIndexToRemove = i;
                    }
                }
            }

            if (largestInternalIndex == -1)
            {
                break; // all gathered are leaves
            }

            gathered.RemoveAt(listIndexToRemove);
            var largestNode = binaryNodes[largestInternalIndex];
            if (largestNode.leftChild != -1)
            {
                gathered.Add(largestNode.leftChild);
            }

            if (largestNode.rightChild != -1)
            {
                gathered.Add(largestNode.rightChild);
            }
        }
    }

    private static int CollapseTo4Ary(UnsafeList<TempBinaryNode> binaryNodes, int binaryNodeIndex, UnsafeList<MeshletHierarchyNode> hierarchyNodes)
    {
        var node = binaryNodes[binaryNodeIndex];
        if (node.leftChild == -1)
        {
            return -1;
        }

        var scope = AllocationManager.CreateStackScope();
        var gathered = new UnsafeList<int>(4, scope.AllocationHandle);

        try
        {
            GatherChildren(binaryNodes, binaryNodeIndex, ref gathered);

            var bvhNode = new MeshletHierarchyNode();

            var minX = new float4(float.PositiveInfinity);
            var minY = new float4(float.PositiveInfinity);
            var minZ = new float4(float.PositiveInfinity);
            var maxX = new float4(float.NegativeInfinity);
            var maxY = new float4(float.NegativeInfinity);
            var maxZ = new float4(float.NegativeInfinity);
            var maxParentError = new float4(0);
            var nodeData = new uint4(0xFFFFFFFF);

            var outNodeIndex = hierarchyNodes.Count;
            hierarchyNodes.Add(bvhNode); // Reserve slot

            for (var i = 0; i < gathered.Count; i++)
            {
                var childIdx = gathered[i];
                var childNode = binaryNodes[childIdx];

                uint data = 0;
                if (childNode.leftChild == -1)
                {
                    data = (uint)childNode.meshletIndex;
                }
                else
                {
                    var child4AryIndex = CollapseTo4Ary(binaryNodes, childIdx, hierarchyNodes);
                    data = (1u << 31) | (uint)child4AryIndex;
                }

                if (i == 0)
                {
                    minX.x = childNode.bounds.Min.x; minY.x = childNode.bounds.Min.y; minZ.x = childNode.bounds.Min.z;
                    maxX.x = childNode.bounds.Max.x; maxY.x = childNode.bounds.Max.y; maxZ.x = childNode.bounds.Max.z;
                    maxParentError.x = childNode.maxParentError;
                    nodeData.x = data;
                }
                else if (i == 1)
                {
                    minX.y = childNode.bounds.Min.x; minY.y = childNode.bounds.Min.y; minZ.y = childNode.bounds.Min.z;
                    maxX.y = childNode.bounds.Max.x; maxY.y = childNode.bounds.Max.y; maxZ.y = childNode.bounds.Max.z;
                    maxParentError.y = childNode.maxParentError;
                    nodeData.y = data;
                }
                else if (i == 2)
                {
                    minX.z = childNode.bounds.Min.x; minY.z = childNode.bounds.Min.y; minZ.z = childNode.bounds.Min.z;
                    maxX.z = childNode.bounds.Max.x; maxY.z = childNode.bounds.Max.y; maxZ.z = childNode.bounds.Max.z;
                    maxParentError.z = childNode.maxParentError;
                    nodeData.z = data;
                }
                else if (i == 3)
                {
                    minX.w = childNode.bounds.Min.x; minY.w = childNode.bounds.Min.y; minZ.w = childNode.bounds.Min.z;
                    maxX.w = childNode.bounds.Max.x; maxY.w = childNode.bounds.Max.y; maxZ.w = childNode.bounds.Max.z;
                    maxParentError.w = childNode.maxParentError;
                    nodeData.w = data;
                }
            }

            bvhNode.minX = minX;
            bvhNode.minY = minY;
            bvhNode.minZ = minZ;
            bvhNode.maxX = maxX;
            bvhNode.maxY = maxY;
            bvhNode.maxZ = maxZ;
            bvhNode.maxParentError = maxParentError;
            bvhNode.nodeData = nodeData;

            hierarchyNodes[outNodeIndex] = bvhNode;
            return outNodeIndex;
        }
        finally
        {
            gathered.Dispose();
            scope.Dispose();
        }
    }

    private unsafe struct BuildClusterLodHierarchyJob : IJob
    {
        public MeshletMeshData* meshletData;

        public readonly void Execute(ref readonly JobExecutionContext ctx)
        {
            using var scope = AllocationManager.CreateStackScope();
            using var meshletIndices = new UnsafeArray<int>(meshletData->meshletCount, scope.AllocationHandle);
            for (var i = 0; i < meshletData->meshletCount; i++)
            {
                meshletIndices[i] = i;
            }

            var binaryNodes = new UnsafeList<TempBinaryNode>(meshletData->meshletCount * 2, scope.AllocationHandle);

            try
            {
                var rootIndex = BuildBinaryTree(ref binaryNodes, meshletIndices, 0, meshletIndices.Length, meshletData->meshlets);

                if (!meshletData->hierarchyNodes.IsCreated)
                {
                    meshletData->hierarchyNodes = new UnsafeList<MeshletHierarchyNode>(meshletData->meshletCount, AllocationHandle.TLSF);
                }

                if (binaryNodes[rootIndex].leftChild == -1)
                {
                    var bvhNode = new MeshletHierarchyNode();
                    bvhNode.minX = new float4(float.PositiveInfinity);
                    bvhNode.minY = new float4(float.PositiveInfinity);
                    bvhNode.minZ = new float4(float.PositiveInfinity);
                    bvhNode.maxX = new float4(float.NegativeInfinity);
                    bvhNode.maxY = new float4(float.NegativeInfinity);
                    bvhNode.maxZ = new float4(float.NegativeInfinity);
                    bvhNode.maxParentError = new float4(0);
                    bvhNode.nodeData = new uint4(0xFFFFFFFF);

                    var childNode = binaryNodes[rootIndex];
                    bvhNode.minX.x = childNode.bounds.Min.x;
                    bvhNode.minY.x = childNode.bounds.Min.y;
                    bvhNode.minZ.x = childNode.bounds.Min.z;
                    bvhNode.maxX.x = childNode.bounds.Max.x;
                    bvhNode.maxY.x = childNode.bounds.Max.y;
                    bvhNode.maxZ.x = childNode.bounds.Max.z;
                    bvhNode.maxParentError.x = childNode.maxParentError;
                    bvhNode.nodeData.x = (uint)childNode.meshletIndex;

                    meshletData->hierarchyNodes.Add(bvhNode);
                }
                else
                {
                    CollapseTo4Ary(binaryNodes, rootIndex, meshletData->hierarchyNodes);
                }
            }
            finally
            {
                binaryNodes.Dispose();
            }
        }
    }

    /// <summary>
    /// Builds a cluster LOD hierarchy from the input meshlet data.
    /// </summary>
    /// <param name="meshletData">The meshlet data.</param>
    public static async Task BuildClusterLodHierarchyAsync(JobScheduler jobScheduler, SharedPtr<MeshletMeshData> meshletData, CancellationToken token)
    {
        if (meshletData.GetRef().meshletCount == 0)
        {
            return;
        }

        JobHandle handle;
        unsafe
        {
            var job = new BuildClusterLodHierarchyJob
            {
                meshletData = meshletData.Get()
            };

            handle = jobScheduler.Schedule(in job);
        }

        await jobScheduler.WaitAsync(handle, token);
    }
}