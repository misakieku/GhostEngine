using Ghost.Core.Graphics;
using Ghost.MeshOptimizer;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Core.Bakers;

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

public struct ClodBounds
{
    public float3 center;
    public float radius;
    public float error;
}

public struct ClodConfig
{
    public nuint maxVertices;
    public nuint minTriangles;
    public nuint maxTriangles;
    public bool partitionSpatial;
    public bool partitionSort;
    public nuint partitionSize;
    public bool clusterSpatial;
    public float clusterFillWeight;
    public float clusterSplitFactor;
    public float simplifyRatio;
    public float simplifyThreshold;
    public float simplifyErrorMergePrevious;
    public float simplifyErrorMergeAdditive;
    public float simplifyErrorFactorSloppy;
    public float simplifyErrorEdgeLimit;
    public bool simplifyPermissive;
    public bool simplifyFallbackPermissive;
    public bool simplifyFallbackSloppy;
    public bool simplifyRegularize;
    public bool optimizeBounds;
    public bool optimizeClusters;
    public int optimizeClustersLevel;
}

public unsafe struct ClodMesh
{
    public float* vertexPositions;
    public nuint vertexCount;
    public nuint vertexPositionsStride;
    public float* vertexAttributes;
    public nuint vertexAttributesStride;
    public float* attributeWeights;
    public nuint attributeCount;
    public uint* indices;
    public nuint indexCount;
    public byte* vertexLock;
    public uint attributeProtectMask;
}

public struct ClodGroup
{
    public int depth;
    public ClodBounds simplified;
}

public unsafe struct ClodCluster
{
    public int refined;
    public ClodBounds bounds;
    public uint* indices;
    public nuint indexCount;
    public uint* uniqueVertices;
    public nuint vertexCount;
    public byte* localIndices;
    public nuint localIndexCount;
}

internal static unsafe partial class MeshProcessor
{
    private delegate int ClodOutputDelegate(MeshletContext context, ClodGroup group, ReadOnlyView<ClodCluster> clusters);

    private static ClodBounds ComputeBounds(ref readonly ClodMesh mesh, ReadOnlySpan<uint> indices, float error)
    {
        fixed (uint* pIndices = indices)
        {
            var bounds = MeshOptApi.ComputeClusterBounds(pIndices, (nuint)indices.Length, mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);
            return new ClodBounds
            {
                center = new float3(bounds.center[0], bounds.center[1], bounds.center[2]),
                radius = bounds.radius,
                error = error
            };
        }
    }

    private static ClodBounds MergeBounds(UnsafeList<Cluster> clusters, UnsafeList<int> group, AllocationHandle allocationHandle)
    {
        using var boundsList = new UnsafeArray<ClodBounds>(group.Count, allocationHandle);
        for (var j = 0; j < group.Count; j++)
        {
            boundsList[j] = clusters[group[j]].bounds;
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

            var posStride = mesh.vertexPositionsStride / sizeof(float);
            var normStride = mesh.vertexAttributesStride / sizeof(float);

            for (nuint t = 0; t < meshlet.triangle_count; t++)
            {
                var i0 = pMeshletTriangles[meshlet.triangle_offset + t * 3 + 0];
                var i1 = pMeshletTriangles[meshlet.triangle_offset + t * 3 + 1];
                var i2 = pMeshletTriangles[meshlet.triangle_offset + t * 3 + 2];

                var v0 = pMeshletVertices[meshlet.vertex_offset + i0];
                var v1 = pMeshletVertices[meshlet.vertex_offset + i1];
                var v2 = pMeshletVertices[meshlet.vertex_offset + i2];

                cluster.localIndices.Add(i0);
                cluster.localIndices.Add(i1);
                cluster.localIndices.Add(i2);

                cluster.indices.Add(v0);
                cluster.indices.Add(v1);
                cluster.indices.Add(v2);
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    internal static void LockBoundary(
        UnsafeArray<byte> locks,
        UnsafeArray<byte> baseLocks,
        UnsafeList<UnsafeList<int>> groups,
        UnsafeList<Cluster> clusters,
        UnsafeArray<uint> remap,
        byte* vertexLock,
        AllocationHandle allocationHandle)
    {
        var pLocks = (byte*)locks.GetUnsafePtr();
        var pBaseLocks = (byte*)baseLocks.GetUnsafePtr();
        var pRemap = (uint*)remap.GetUnsafePtr();
        var vertexCount = locks.Length;

        Unsafe.CopyBlock(pLocks, pBaseLocks, (uint)vertexCount);

        uint maxPosId = 0;
        for (var v = 0; v < vertexCount; v++)
        {
            if (pRemap[v] > maxPosId)
            {
                maxPosId = pRemap[v];
            }
        }

        using var owner = new UnsafeArray<int>((int)maxPosId + 1, allocationHandle);
        using var lockedPos = new UnsafeArray<byte>((int)maxPosId + 1, allocationHandle, AllocationOption.Clear);
        var pOwner = (int*)owner.GetUnsafePtr();
        var pLockedPos = (byte*)lockedPos.GetUnsafePtr();

        for (var i = 0; i <= (int)maxPosId; i++)
        {
            pOwner[i] = -1;
        }

        for (var gi = 0; gi < groups.Count; gi++)
        {
            var group = groups[gi];
            for (var j = 0; j < group.Count; j++)
            {
                var cluster = clusters[group[j]];
                for (var k = 0; k < cluster.uniqueVertices.Count; k++)
                {
                    uint v = cluster.uniqueVertices[k];
                    uint pid = pRemap[(int)v];
                    if (pOwner[(int)pid] == -1)
                    {
                        pOwner[(int)pid] = gi;
                    }
                    else if (pOwner[(int)pid] != gi)
                    {
                        pLockedPos[(int)pid] = 1;
                    }
                }
            }
        }

        for (var v = 0; v < vertexCount; v++)
        {
            if (pLockedPos[(int)pRemap[v]] != 0)
            {
                pLocks[v] |= (byte)SimplifyVertexOptions.Lock;
            }

            if (vertexLock != null)
            {
                pLocks[v] |= vertexLock[v];
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

        for (var i = 0; i < pending.Count; i++)
        {
            var cluster = clusters[pending[i]];
            clusterCounts.Add((uint)cluster.indices.Count);
            for (var j = 0; j < cluster.indices.Count; j++)
            {
                clusterIndices.Add(((uint*)remap.GetUnsafePtr())[(int)cluster.indices[j]]);
            }
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
                bounds = srcCluster.bounds,
                indices = (uint*)srcCluster.indices.GetUnsafePtr(),
                indexCount = (nuint)srcCluster.indices.Count,
                uniqueVertices = (uint*)srcCluster.uniqueVertices.GetUnsafePtr(),
                vertexCount = srcCluster.vertices,
                localIndices = (byte*)srcCluster.localIndices.GetUnsafePtr(),
                localIndexCount = (nuint)srcCluster.localIndices.Count
            });
        }

        var clodGroup = new ClodGroup { depth = depth, simplified = simplified };
        return outputCallback != null
            ? outputCallback(outputContext, clodGroup, groupClusters.AsReadOnly())
            : -1;
    }

    private struct SloppyVertex
    {
        public float x, y, z;
        public uint id;
    }

    private static void SimplifyFallback(ref UnsafeArray<uint> lod, ref readonly ClodMesh mesh, ReadOnlyView<uint> indices, ReadOnlyView<byte> locks, nuint targetCount, float* error, AllocationHandle allocationHandle)
    {
        using var subset = new UnsafeArray<SloppyVertex>(indices.Count, allocationHandle);
        using var subsetLocks = new UnsafeArray<byte>(indices.Count, allocationHandle);

        lod.Resize(indices.Count);

        var positionsStride = mesh.vertexPositionsStride / sizeof(float);

        for (var i = 0; i < indices.Count; ++i)
        {
            var v = indices[i];
            subset[i].x = mesh.vertexPositions[v * positionsStride + 0];
            subset[i].y = mesh.vertexPositions[v * positionsStride + 1];
            subset[i].z = mesh.vertexPositions[v * positionsStride + 2];
            subset[i].id = v;

            subsetLocks[i] = locks[v];
            lod[i] = (uint)i;
        }

        var newSize = MeshOptApi.SimplifySloppy(
            (uint*)lod.GetUnsafePtr(),
            (uint*)lod.GetUnsafePtr(),
            (nuint)lod.Count,
            (float*)subset.GetUnsafePtr(),
            (nuint)subset.Count,
            (nuint)sizeof(SloppyVertex),
            (byte*)subsetLocks.GetUnsafePtr(),
            targetCount,
            float.MaxValue,
            error);

        lod.Resize((int)newSize);
        *error *= MeshOptApi.SimplifyScale((float*)subset.GetUnsafePtr(), (nuint)subset.Count, (nuint)sizeof(SloppyVertex));

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
            var posStride = mesh.vertexPositionsStride / sizeof(float);

            for (var i = 0; i < indices.Count; i += 3)
            {
                uint a = pIdx[i], b = pIdx[i + 1], c = pIdx[i + 2];
                var va = mesh.vertexPositions + (a * posStride);
                var vb = mesh.vertexPositions + (b * posStride);
                var vc = mesh.vertexPositions + (c * posStride);

                float dx = va[0] - vb[0], dy = va[1] - vb[1], dz = va[2] - vb[2];
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

    private static nuint Build(ref readonly ClodConfig config, ref readonly ClodMesh mesh, MeshletContext outputContext, ClodOutputDelegate? outputCallback, AllocationHandle allocationHandle)
    {
        using var locks = new UnsafeArray<byte>((int)mesh.vertexCount, allocationHandle);
        using var baseLocks = new UnsafeArray<byte>((int)mesh.vertexCount, allocationHandle, AllocationOption.Clear);
        using var remap = new UnsafeArray<uint>((int)mesh.vertexCount, allocationHandle);

        MeshOptApi.GeneratePositionRemap((uint*)remap.GetUnsafePtr(), mesh.vertexPositions, mesh.vertexCount, mesh.vertexPositionsStride);

        if (config.simplifyPermissive && mesh.vertexAttributes != null && mesh.attributeCount > 0)
        {
            var pBaseLocks = (byte*)baseLocks.GetUnsafePtr();
            var pRemap = (uint*)remap.GetUnsafePtr();
            var normStride = mesh.vertexAttributesStride / sizeof(float);

            for (nuint i = 0; i < mesh.vertexCount; i++)
            {
                var r = pRemap[(int)i];
                if (r == (uint)i)
                {
                    continue;
                }

                var ni = mesh.vertexAttributes + i * normStride;
                var nr = mesh.vertexAttributes + r * normStride;
                var differ = false;
                for (nuint a = 0; a < mesh.attributeCount; a++)
                {
                    if (ni[a] != nr[a])
                    {
                        differ = true;
                        break;
                    }
                }

                if (differ)
                {
                    pBaseLocks[i] |= (byte)SimplifyVertexOptions.Protect;
                    pBaseLocks[r] |= (byte)SimplifyVertexOptions.Protect;
                }
            }
        }

        using var clusters = Clusterize(in config, in mesh, mesh.indices, mesh.indexCount, allocationHandle);

        for (var i = 0; i < clusters.Count; i++)
        {
            clusters[i].bounds = ComputeBounds(in mesh, clusters[i].indices.AsSpan(), 0.0f);
        }

        using var pending = new UnsafeList<int>(clusters.Count, allocationHandle);
        for (var i = 0; i < clusters.Count; i++)
        {
            pending.Add(i);
        }

        var depth = 0;

        while (pending.Count > 1)
        {
            using var groups = Partition(in config, in mesh, clusters, pending, remap, allocationHandle);
            pending.Clear();

            LockBoundary(locks, baseLocks, groups, clusters, remap, mesh.vertexLock, allocationHandle);

            for (var i = 0; i < groups.Count; i++)
            {
                using var merged = new UnsafeList<uint>(groups[i].Count * (int)config.maxTriangles * 3, allocationHandle);
                for (var j = 0; j < groups[i].Count; j++)
                {
                    var clusterIndices = clusters[groups[i][j]].indices;
                    merged.AddRange(clusterIndices.AsSpan());
                }

                var targetSize = (nuint)(merged.Count / 3 * config.simplifyRatio * 3.0f);
                var bounds = MergeBounds(clusters, groups[i], allocationHandle);

                var error = 0.0f;
                using var simplified = Simplify(in config, in mesh, merged.AsReadOnly(), locks.AsReadOnly(), targetSize, &error, allocationHandle);

                if ((nuint)simplified.Length > (nuint)(merged.Count * config.simplifyThreshold))
                {
                    bounds.error = Math.Max(bounds.error * 2.0f, bounds.radius * 2.0f);
                    OutputGroup(in config, in mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback, allocationHandle);
                    continue;
                }

                bounds.error = Math.Max(bounds.error * config.simplifyErrorMergePrevious, error) + error * config.simplifyErrorMergeAdditive;

                var refined = OutputGroup(in config, in mesh, clusters, groups[i], bounds, depth, outputContext, outputCallback, allocationHandle);

                for (var j = 0; j < groups[i].Count; j++)
                {
                    clusters[groups[i][j]].Dispose();
                }

                using var split = Clusterize(in config, in mesh, (uint*)simplified.GetUnsafePtr(), (nuint)simplified.Length, allocationHandle);
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
            var bounds = (pending.Count == 1)
                ? clusters[pending[0]].bounds
                : MergeBounds(clusters, pending, allocationHandle);
            bounds.error = Math.Max(bounds.error * 2.0f, bounds.radius * 2.0f);
            OutputGroup(in config, in mesh, clusters, pending, bounds, depth, outputContext, outputCallback, allocationHandle);
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
        public AllocationHandle allocationHandle;
        public ClodMesh mesh;
    }

    private static int MeshletOutputCallback(MeshletContext context, ClodGroup group, ReadOnlyView<ClodCluster> clusters)
    {
        var meshletData = context.data;
        var materialIndex = context.materialIndex;
        var handle = context.allocationHandle;

        if (!meshletData->groups.IsCreated)
        {
            meshletData->groups = new UnsafeList<MeshletGroup>(16, handle);
        }

        if (!meshletData->meshlets.IsCreated)
        {
            meshletData->meshlets = new UnsafeList<Meshlet>(64, handle);
        }

        if (!meshletData->meshletVertices.IsCreated)
        {
            meshletData->meshletVertices = new UnsafeList<uint>(128, handle);
        }

        if (!meshletData->meshletTriangles.IsCreated)
        {
            meshletData->meshletTriangles = new UnsafeList<uint>(128, handle);
        }

        var groupIndex = (uint)meshletData->groups.Count;
        var groupMin = new float3(float.MaxValue);
        var groupMax = new float3(float.MinValue);

        for (var i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            var triangleCount = cluster.localIndexCount / 3;

            var optBounds = MeshOptApi.ComputeMeshletBounds(
                cluster.uniqueVertices,
                cluster.localIndices,
                triangleCount,
                context.mesh.vertexPositions,
                context.mesh.vertexCount,
                context.mesh.vertexPositionsStride
            );

            var optCenter = new float3(optBounds.center[0], optBounds.center[1], optBounds.center[2]);
            var optRadius = optBounds.radius;

            var meshletMin = new float3(float.MaxValue);
            var meshletMax = new float3(float.MinValue);
            for (nuint v = 0; v < cluster.vertexCount; v++)
            {
                var vIndex = cluster.uniqueVertices[v];
                var vPtr = (float*)((byte*)context.mesh.vertexPositions + vIndex * context.mesh.vertexPositionsStride);
                var pos = new float3(vPtr[0], vPtr[1], vPtr[2]);
                meshletMin = math.min(meshletMin, pos);
                meshletMax = math.max(meshletMax, pos);
            }

            if (cluster.vertexCount == 0)
            {
                meshletMin = optCenter - optRadius;
                meshletMax = optCenter + optRadius;
            }

            groupMin = math.min(groupMin, meshletMin);
            groupMax = math.max(groupMax, meshletMax);

            var clusterSphere = (group.depth == 0)
                ? new SphereBounds(optCenter, optRadius)
                : new SphereBounds(cluster.bounds.center, cluster.bounds.radius);

            var parentSphere = new SphereBounds(group.simplified.center, group.simplified.radius);

            var meshlet = new Meshlet
            {
                boundingSphere = clusterSphere,
                parentBoundingSphere = parentSphere,
                boundingBox = new AABB(meshletMin, meshletMax),
                vertexCount = (byte)cluster.vertexCount,
                triangleCount = (byte)triangleCount,
                vertexOffset = (uint)meshletData->meshletVertices.Count,
                triangleOffset = (uint)meshletData->meshletTriangles.Count,
                groupIndex = groupIndex,
                clusterError = (group.depth == 0) ? 0.0f : cluster.bounds.error,
                parentError = group.simplified.error,
                localMaterialIndex = (byte)materialIndex,
                lodLevel = (byte)group.depth
            };
            meshletData->meshlets.Add(meshlet);

            for (nuint j = 0; j < cluster.vertexCount; j++)
            {
                meshletData->meshletVertices.Add(cluster.uniqueVertices[j]);
            }

            for (nuint j = 0; j < triangleCount; j++)
            {
                uint i0 = cluster.localIndices[j * 3 + 0];
                uint i1 = cluster.localIndices[j * 3 + 1];
                uint i2 = cluster.localIndices[j * 3 + 2];
                var packedTriangle = i0 | (i1 << 8) | (i2 << 16);
                meshletData->meshletTriangles.Add(packedTriangle);
            }
        }

        if (clusters.Count == 0)
        {
            groupMin = group.simplified.center - group.simplified.radius;
            groupMax = group.simplified.center + group.simplified.radius;
        }

        var currentLod = (uint)group.depth;
        var groupSphere = new SphereBounds(group.simplified.center, group.simplified.radius);

        var meshletStart = meshletData->meshlets.Count - clusters.Count;
        var meshletGroup = new MeshletGroup
        {
            boundingSphere = groupSphere,
            boundingBox = new AABB(groupMin, groupMax),
            parentError = group.simplified.error,
            meshletStartIndex = (uint)meshletStart,
            meshletCount = (uint)clusters.Count,
            lodLevel = currentLod
        };
        meshletData->groups.Add(meshletGroup);

        return meshletData->groups.Count - 1;
    }

    public static DisposablePtr<MeshletMeshData> BuildMeshlets(
        ReadOnlyView<Vertex> vertices,
        ReadOnlyView<uint> indices,
        ReadOnlyView<MaterialPartInfo> parts,
        MeshBakeSettings settings,
        AllocationHandle allocationHandle)
    {
        var config = new ClodConfig
        {
            maxVertices = (nuint)settings.MaxVerticesPerMeshlet,
            minTriangles = (nuint)settings.MinTrianglesPerMeshlet,
            maxTriangles = (nuint)settings.MaxTrianglesPerMeshlet,

            partitionSpatial = true,
            partitionSize = 16,

            clusterSpatial = false,
            clusterSplitFactor = 2.0f,

            optimizeClusters = settings.OptimizeClusters,
            optimizeClustersLevel = 1,

            simplifyRatio = settings.SimplifyRatio,
            simplifyThreshold = settings.SimplifyThreshold,
            simplifyErrorMergePrevious = 1.0f,
            simplifyErrorFactorSloppy = 2.0f,
            simplifyPermissive = true,
            simplifyFallbackPermissive = false,
            simplifyFallbackSloppy = true,
        };

        var meshletData = (MeshletMeshData*)NativeMemory.AllocZeroed((nuint)sizeof(MeshletMeshData));

        try
        {
            var weights = stackalloc float[] { 0.5f, 0.5f, 0.5f, 0.1f, 0.1f };
            for (var i = 0; i < parts.Length; i++)
            {
                ref readonly var part = ref parts[i];
                var clodMesh = new ClodMesh
                {
                    vertexPositions = (float*)Unsafe.AsPointer(in vertices[0].position),
                    vertexCount = (nuint)vertices.Count,
                    vertexPositionsStride = (nuint)sizeof(Vertex),
                    vertexAttributes = (float*)Unsafe.AsPointer(in vertices[0].normal),
                    vertexAttributesStride = (nuint)sizeof(Vertex),
                    attributeWeights = weights,
                    attributeCount = 5,
                    indices = (uint*)indices.GetUnsafePtr() + part.indexStart,
                    indexCount = (nuint)part.indexCount,
                    attributeProtectMask = 0,
                };

                var context = new MeshletContext
                {
                    data = meshletData,
                    materialIndex = part.materialIndex,
                    allocationHandle = allocationHandle,
                    mesh = clodMesh,
                };

                Build(in config, in clodMesh, context, MeshletOutputCallback, allocationHandle);
            }

            meshletData->meshletCount = meshletData->meshlets.IsCreated ? meshletData->meshlets.Count : 0;
            meshletData->meshletGroupCount = meshletData->groups.IsCreated ? meshletData->groups.Count : 0;

            if (meshletData->groups.IsCreated && meshletData->groups.Count > 0)
            {
                var maxLodLevel = 0u;
                for (var j = 0; j < meshletData->groups.Count; j++)
                {
                    maxLodLevel = Math.Max(maxLodLevel, meshletData->groups[j].lodLevel);
                }

                meshletData->lodLevelCount = (int)maxLodLevel + 1;
            }

            var maxMaterialSlot = 0;
            for (var j = 0; j < parts.Length; j++)
            {
                maxMaterialSlot = Math.Max(maxMaterialSlot, parts[j].materialIndex);
            }

            meshletData->materialSlotCount = maxMaterialSlot + 1;

            // Build hierarchical BVH for Work Graph dual-error culling
            BuildClusterLodHierarchy(meshletData, allocationHandle, 8);

            return new DisposablePtr<MeshletMeshData>(meshletData);
        }
        catch
        {
            NativeMemory.Free(meshletData);
            throw;
        }
    }

    private struct TempTreeNode : IDisposable
    {
        public SphereBounds bounds;
        public float error;
        public int groupIndex;
        public UnsafeList<int> children;

        public void Dispose()
        {
            children.Dispose();
        }
    }

    private static SphereBounds EncloseSphere(SphereBounds a, SphereBounds b)
    {
        if (a.Radius <= 0.0f)
        {
            return b;
        }

        if (b.Radius <= 0.0f)
        {
            return a;
        }

        var d = b.Center - a.Center;
        var dist = math.length(d);
        if (dist < 1e-6f)
        {
            return new SphereBounds(a.Center, Math.Max(a.Radius, b.Radius));
        }
        if (dist + b.Radius <= a.Radius)
        {
            return a;
        }

        if (dist + a.Radius <= b.Radius)
        {
            return b;
        }

        var newRadius = (dist + a.Radius + b.Radius) * 0.5f;
        var newCenter = a.Center + d * ((newRadius - a.Radius) / dist);
        return new SphereBounds(newCenter, newRadius);
    }

    /// <summary>
    /// Builds a hierarchical BVH for continuous LOD and Dual-Error culling in Work Graphs.
    /// Node 0 is the root node of the hierarchy.
    /// </summary>
    public static void BuildClusterLodHierarchy(MeshletMeshData* meshletData, AllocationHandle allocationHandle, nuint maxFanout = 8)
    {
        if (!meshletData->groups.IsCreated || meshletData->groups.Count == 0)
        {
            return;
        }

        // If the hierarchy has already been built (e.g. during BuildMeshlets), do not rebuild
        if (meshletData->hierarchyNodes.IsCreated && meshletData->hierarchyNodes.Count > 0)
        {
            return;
        }

        var groupCount = meshletData->groups.Count;
        var levelCount = meshletData->lodLevelCount;

        // If mesh only has 1 group, create a single root leaf node directly
        if (groupCount == 1)
        {
            if (!meshletData->hierarchyNodes.IsCreated)
            {
                meshletData->hierarchyNodes = new UnsafeList<MeshletHierarchyNode>(1, allocationHandle);
            }
            meshletData->hierarchyNodes.Clear();
            var bounds = meshletData->groups[0].boundingSphere;
            var safeError = Math.Max(meshletData->groups[0].parentError, Math.Max(bounds.Radius * 2.0f, 1.0f));
            meshletData->hierarchyNodes.Add(new MeshletHierarchyNode
            {
                bounds = bounds,
                error = safeError,
                groupIndex = 0,
                childOffset = 0,
                childCount = 0
            });
            return;
        }

        var tempNodes = new UnsafeList<TempTreeNode>(groupCount * 2, allocationHandle);
        using var lodRoots = new UnsafeList<int>(levelCount, allocationHandle);

        try
        {
            // 1. Group all meshlet groups by LOD level
            using var lodGroupLists = new UnsafeArray<UnsafeList<int>>(levelCount, allocationHandle);
            for (var i = 0; i < levelCount; i++)
            {
                lodGroupLists[i] = new UnsafeList<int>(16, allocationHandle);
            }

            for (var g = 0; g < groupCount; g++)
            {
                var lod = (int)meshletData->groups[g].lodLevel;
                if (lod >= 0 && lod < levelCount)
                {
                    lodGroupLists[lod].Add(g);
                }
            }

            // 2. Build bottom-up BVH for each LOD level (from coarsest to finest)
            for (var lvl = levelCount - 1; lvl >= 0; lvl--)
            {
                var groupsInLod = lodGroupLists[lvl];
                if (groupsInLod.Count == 0)
                {
                    continue;
                }

                // Create leaf nodes for each group in this LOD
                var currentLevelNodes = new UnsafeList<int>(groupsInLod.Count, allocationHandle);
                for (var i = 0; i < groupsInLod.Count; i++)
                {
                    var g = groupsInLod[i];
                    var grp = meshletData->groups[g];
                    var leafIdx = tempNodes.Count;

                    tempNodes.Add(new TempTreeNode
                    {
                        bounds = grp.boundingSphere,
                        error = grp.parentError,
                        groupIndex = g,
                        children = new UnsafeList<int>(0, allocationHandle)
                    });
                    currentLevelNodes.Add(leafIdx);
                }

                // Build bottom-up BVH for this LOD until a single root remains
                while (currentLevelNodes.Count > 1)
                {
                    var nodeCount = currentLevelNodes.Count;
                    var nextLevelNodes = new UnsafeList<int>((nodeCount + (int)maxFanout - 1) / (int)maxFanout, allocationHandle);

                    if ((nuint)nodeCount <= maxFanout)
                    {
                        // Small enough to fit directly in one parent node
                        var childList = new UnsafeList<int>(nodeCount, allocationHandle);
                        childList.AddRange(currentLevelNodes.AsSpan());

                        var mergedBounds = tempNodes[currentLevelNodes[0]].bounds;
                        var maxError = tempNodes[currentLevelNodes[0]].error;

                        for (var j = 1; j < nodeCount; j++)
                        {
                            var childIdx = currentLevelNodes[j];
                            mergedBounds = EncloseSphere(mergedBounds, tempNodes[childIdx].bounds);
                            maxError = Math.Max(maxError, tempNodes[childIdx].error);
                        }

                        var parentIdx = tempNodes.Count;
                        tempNodes.Add(new TempTreeNode
                        {
                            bounds = mergedBounds,
                            error = maxError,
                            groupIndex = -1,
                            children = childList
                        });
                        nextLevelNodes.Add(parentIdx);
                    }
                    else
                    {
                        // Spatial clustering using meshopt_spatialClusterPoints
                        using var centers = new UnsafeArray<float>(nodeCount * 3, allocationHandle);
                        for (var i = 0; i < nodeCount; i++)
                        {
                            var n = tempNodes[currentLevelNodes[i]];
                            centers[i * 3 + 0] = n.bounds.Center.x;
                            centers[i * 3 + 1] = n.bounds.Center.y;
                            centers[i * 3 + 2] = n.bounds.Center.z;
                        }

                        using var clusterIndices = new UnsafeArray<uint>(nodeCount, allocationHandle);
                        MeshOptApi.SpatialClusterPoints(
                            (uint*)clusterIndices.GetUnsafePtr(),
                            (float*)centers.GetUnsafePtr(),
                            (nuint)nodeCount,
                            (nuint)(sizeof(float) * 3),
                            maxFanout
                        );

                        var clusterCount = (nodeCount + (int)maxFanout - 1) / (int)maxFanout;
                        for (var c = 0; c < clusterCount; c++)
                        {
                            var start = c * (int)maxFanout;
                            var size = Math.Min((int)maxFanout, nodeCount - start);
                            if (size <= 0) continue;

                            var childList = new UnsafeList<int>(size, allocationHandle);
                            var firstChildIdx = currentLevelNodes[(int)clusterIndices[start]];
                            childList.Add(firstChildIdx);

                            var mergedBounds = tempNodes[firstChildIdx].bounds;
                            var maxError = tempNodes[firstChildIdx].error;

                            for (var j = 1; j < size; j++)
                            {
                                var childIdx = currentLevelNodes[(int)clusterIndices[start + j]];
                                childList.Add(childIdx);
                                mergedBounds = EncloseSphere(mergedBounds, tempNodes[childIdx].bounds);
                                maxError = Math.Max(maxError, tempNodes[childIdx].error);
                            }

                            var parentIdx = tempNodes.Count;
                            tempNodes.Add(new TempTreeNode
                            {
                                bounds = mergedBounds,
                                error = maxError,
                                groupIndex = -1,
                                children = childList
                            });
                            nextLevelNodes.Add(parentIdx);
                        }
                    }

                    currentLevelNodes.Dispose();
                    currentLevelNodes = nextLevelNodes;
                }

                lodRoots.Add(currentLevelNodes[0]);
                currentLevelNodes.Dispose();
            }

            for (var i = 0; i < levelCount; i++)
            {
                lodGroupLists[i].Dispose();
            }

            // 3. Connect LOD roots into a single Top-Level BVH
            int rootNodeIdx;
            if (lodRoots.Count == 1)
            {
                rootNodeIdx = lodRoots[0];
            }
            else
            {
                var currentRoots = new UnsafeList<int>(lodRoots.Count, allocationHandle);
                currentRoots.AddRange(lodRoots.AsSpan());

                while (currentRoots.Count > 1)
                {
                    var count = currentRoots.Count;
                    var nextRoots = new UnsafeList<int>((count + (int)maxFanout - 1) / (int)maxFanout, allocationHandle);

                    for (var i = 0; i < count; i += (int)maxFanout)
                    {
                        var chunk = Math.Min((int)maxFanout, count - i);
                        var childList = new UnsafeList<int>(chunk, allocationHandle);

                        var mergedBounds = tempNodes[currentRoots[i]].bounds;
                        var maxError = tempNodes[currentRoots[i]].error;
                        childList.Add(currentRoots[i]);

                        for (var j = 1; j < chunk; j++)
                        {
                            var childIdx = currentRoots[i + j];
                            childList.Add(childIdx);
                            mergedBounds = EncloseSphere(mergedBounds, tempNodes[childIdx].bounds);
                            maxError = Math.Max(maxError, tempNodes[childIdx].error);
                        }

                        // Top-level internal nodes take the maximum error of their children
                        var parentIdx = tempNodes.Count;
                        tempNodes.Add(new TempTreeNode
                        {
                            bounds = mergedBounds,
                            error = maxError,
                            groupIndex = -1,
                            children = childList
                        });
                        nextRoots.Add(parentIdx);
                    }

                    currentRoots.Dispose();
                    currentRoots = nextRoots;
                }

                rootNodeIdx = currentRoots[0];
                currentRoots.Dispose();
            }

            // 4. Linearize tree in BFS order (Root is placed at index 0)
            if (!meshletData->hierarchyNodes.IsCreated)
            {
                meshletData->hierarchyNodes = new UnsafeList<MeshletHierarchyNode>(tempNodes.Count, allocationHandle);
            }
            meshletData->hierarchyNodes.Clear();
            meshletData->hierarchyNodes.Resize(tempNodes.Count);

            using var bfsQueue = new UnsafeList<int>(tempNodes.Count, allocationHandle);
            bfsQueue.Add(rootNodeIdx);

            var nextLinearChildIdx = 1;
            var writePtr = 0;

            while (writePtr < bfsQueue.Count)
            {
                var currentTempIdx = bfsQueue[writePtr];
                var linearIdx = writePtr;
                writePtr++;

                var temp = tempNodes[currentTempIdx];
                var childCount = (uint)temp.children.Count;
                uint childOffset = 0;

                if (childCount > 0)
                {
                    childOffset = (uint)nextLinearChildIdx;
                    for (var c = 0; c < childCount; c++)
                    {
                        bfsQueue.Add(temp.children[c]);
                    }
                    nextLinearChildIdx += (int)childCount;
                }

                if (linearIdx >= meshletData->hierarchyNodes.Count)
                {
                    meshletData->hierarchyNodes.Resize(linearIdx + 1);
                }

                meshletData->hierarchyNodes[linearIdx] = new MeshletHierarchyNode
                {
                    bounds = temp.bounds,
                    error = temp.error,
                    groupIndex = temp.groupIndex,
                    childOffset = childOffset,
                    childCount = childCount
                };
            }

            meshletData->hierarchyNodes.UnsafeSetCount(bfsQueue.Count);
        }
        finally
        {
            for (var i = 0; i < tempNodes.Count; i++)
            {
                tempNodes[i].Dispose();
            }
            tempNodes.Dispose();
        }
    }
}
