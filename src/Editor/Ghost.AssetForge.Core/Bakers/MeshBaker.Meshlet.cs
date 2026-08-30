using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
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
                bounds = (config.optimizeBounds && srcCluster.refined != -1)
                    ? ComputeBounds(in mesh, srcCluster.indices.AsSpan(), srcCluster.bounds.error)
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
            var posStride = mesh.vertexPositionsStride / (nuint)sizeof(float);

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
        using var locks = new UnsafeArray<byte>((int)mesh.vertexCount, allocationHandle, AllocationOption.Clear);
        using var remap = new UnsafeArray<uint>((int)mesh.vertexCount, allocationHandle);

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

            LockBoundary(locks, groups, clusters, remap, mesh.vertexLock);

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
                    bounds.error = float.MaxValue;
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
            var bounds = clusters[pending[0]].bounds;
            bounds.error = float.MaxValue;
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

            for (nuint j = 0; j < cluster.vertexCount; j++)
            {
                meshletData->meshletVertices.Add(cluster.uniqueVertices[j]);
            }

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
                    indices = (uint*)indices.GetUnsafePtr() + part.indexStart,
                    indexCount = (nuint)part.indexCount,
                    attributeProtectMask = 0,
                };

                var context = new MeshletContext
                {
                    data = meshletData,
                    materialIndex = part.materialIndex,
                    allocationHandle = allocationHandle
                };

                Build(in config, in clodMesh, context, MeshletOutputCallback, allocationHandle);
            }

            meshletData->meshletCount = meshletData->meshlets.IsCreated ? meshletData->meshlets.Count : 0;

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

            return new DisposablePtr<MeshletMeshData>(meshletData);
        }
        catch
        {
            NativeMemory.Free(meshletData);
            throw;
        }
    }

    public static nuint ClodBuildHierarchyBound(nuint groupCount, nuint nodeWidth, nuint levelCount)
    {
        var total = levelCount;
        for (var frontier = groupCount; frontier > 1; frontier = (frontier + nodeWidth - 1) / nodeWidth)
        {
            total += frontier + levelCount;
        }

        return total;
    }

    private static MeshletHierarchyNode MergeHierarchyNodes(MeshletHierarchyNode* nodes, uint offset, uint count)
    {
        var pNodes = nodes + offset;
        var pCenters = (float*)pNodes;
        var pRadii = (float*)pNodes + 3;
        var stride = (nuint)sizeof(MeshletHierarchyNode);

        var merged = MeshOptApi.ComputeSphereBounds(
            pCenters,
            count,
            stride,
            pRadii,
            stride
        );

        var maxError = 0.0f;
        for (uint j = 0; j < count; j++)
        {
            maxError = Math.Max(maxError, pNodes[j].error);
        }

        return new MeshletHierarchyNode
        {
            bounds = new SphereBounds(new float3(merged.center[0], merged.center[1], merged.center[2]), merged.radius),
            error = maxError,
            groupIndex = -1,
            childOffset = offset,
            childCount = count
        };
    }

    /// <summary>
    /// Builds a spatial cluster hierarchy over groups per DAG level using bottom-up spatial clustering.
    /// Matches meshoptimizer's clodBuildHierarchy.
    /// </summary>
    public static void BuildClusterLodHierarchy(MeshletMeshData* meshletData, AllocationHandle allocationHandle, nuint nodeWidth = 4)
    {
        if (!meshletData->groups.IsCreated || meshletData->groups.Count == 0)
        {
            return;
        }

        var groupCount = (nuint)meshletData->groups.Count;
        var levelCount = (nuint)meshletData->lodLevelCount;
        var maxNodes = ClodBuildHierarchyBound(groupCount, nodeWidth, levelCount);

        if (!meshletData->hierarchyNodes.IsCreated)
        {
            meshletData->hierarchyNodes = new UnsafeList<MeshletHierarchyNode>((int)maxNodes, allocationHandle);
        }

        meshletData->hierarchyNodes.Resize((int)maxNodes);
        var pNodes = (MeshletHierarchyNode*)meshletData->hierarchyNodes.GetUnsafePtr();

        var offset = levelCount;

        using var row = new UnsafeList<MeshletHierarchyNode>((int)groupCount, allocationHandle);
        using var order = new UnsafeArray<uint>((int)groupCount, allocationHandle);

        for (nuint level = 0; level < levelCount; ++level)
        {
            row.Clear();
            for (var i = 0; i < (int)groupCount; ++i)
            {
                if (meshletData->groups[i].lodLevel == (uint)level)
                {
                    row.Add(new MeshletHierarchyNode
                    {
                        bounds = meshletData->groups[i].boundingSphere,
                        error = meshletData->groups[i].parentError,
                        groupIndex = i,
                        childOffset = 0,
                        childCount = 0
                    });
                }
            }

            if (row.Count == 0)
            {
                pNodes[level] = new MeshletHierarchyNode
                {
                    bounds = default,
                    error = float.MaxValue,
                    groupIndex = -1,
                    childOffset = 0,
                    childCount = 0
                };
                continue;
            }

            while (row.Count > 1)
            {
                var count = (nuint)row.Count;
                var pCenters = (float*)row.GetUnsafePtr();
                MeshOptApi.SpatialClusterPoints((uint*)order.GetUnsafePtr(), pCenters, count, (nuint)sizeof(MeshletHierarchyNode), nodeWidth);
                for (nuint i = 0; i < count; ++i)
                {
                    pNodes[offset + i] = row[(int)order[(int)i]];
                }

                row.Clear();
                for (nuint i = 0; i < count; i += nodeWidth)
                {
                    var children = (uint)Math.Min(nodeWidth, count - i);
                    row.Add(MergeHierarchyNodes(pNodes, (uint)(offset + i), children));
                }

                offset += count;
            }

            pNodes[level] = row[0];
        }

        meshletData->hierarchyNodes.UnsafeSetCount((int)offset);
    }
}
