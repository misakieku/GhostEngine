using Ghost.Core.Graphics;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Core.Bakers;

/// <summary>
/// Result of bundling meshlets into 128KB streaming pages.
/// </summary>
public struct PackedMeshPages : IDisposable
{
    public UnsafeList<Meshlet> reorderedMeshlets;
    public UnsafeList<PageDirectoryEntry> pageDirectory;
    public byte[][] pageBuffers;

    public void Dispose()
    {
        reorderedMeshlets.Dispose();
        pageDirectory.Dispose();
    }
}

/// <summary>
/// Bundles cluster meshlets, local packed vertices, and micro-triangles into 128KB self-contained pages.
/// </summary>
public static unsafe class PagePacker
{
    public static PackedMeshPages PackPages(MeshletMeshData* meshletData, AllocationHandle handle)
    {
        var totalMeshletCount = meshletData->meshlets.Count;
        var groupCount = meshletData->groups.Count;
        var levelCount = meshletData->lodLevelCount;

        var reorderedMeshlets = new UnsafeList<Meshlet>(totalMeshletCount, handle);
        var pageDirectory = new UnsafeList<PageDirectoryEntry>(16, handle);
        var pageBuffers = new List<byte[]>();

        // Determine how many coarse LOD levels fit into Root Page 0 (128 KB)
        var rootLod = Math.Max(0, levelCount - 1);
        var rootEstimatedBytes = sizeof(PageHeader);
        for (var l = levelCount - 1; l >= 0; l--)
        {
            var lodBytes = 0;
            for (var g = 0; g < groupCount; g++)
            {
                if (meshletData->groups[g].lodLevel == (uint)l)
                {
                    lodBytes += EstimateGroupBytes(meshletData->groups[g], meshletData);
                }
            }

            if (rootEstimatedBytes + lodBytes <= MeshletPageConstants.PAGE_SIZE)
            {
                rootEstimatedBytes += lodBytes;
                rootLod = l;
            }
            else
            {
                break;
            }
        }

        // Group indices segregated into Root groups and Bulk groups
        using var rootGroupIndices = new UnsafeList<int>(groupCount, handle);
        using var bulkGroupIndices = new UnsafeList<int>(groupCount, handle);

        // Populate root vs bulk groups
        for (var g = 0; g < groupCount; g++)
        {
            if (meshletData->groups[g].lodLevel >= (uint)rootLod)
            {
                rootGroupIndices.Add(g);
            }
            else
            {
                bulkGroupIndices.Add(g);
            }
        }

        // If no root groups found, fallback all groups to root
        if (rootGroupIndices.Count == 0)
        {
            for (var g = 0; g < groupCount; g++)
            {
                rootGroupIndices.Add(g);
            }
            bulkGroupIndices.Clear();
        }

        // --- Pack Page 0: Root Page (Pinned coarsest LOD) ---
        PackSinglePage(
            pageIndex: 0,
            isRootPage: true,
            groupIndices: rootGroupIndices.AsSpan(),
            predominantLod: (byte)rootLod,
            meshletData: meshletData,
            reorderedMeshlets: ref reorderedMeshlets,
            pageDirectory: ref pageDirectory,
            pageBuffers: pageBuffers,
            handle: handle);

        // --- Pack Bulk Pages 1..N (Streamable fine LODs) ---
        if (bulkGroupIndices.Count > 0)
        {
            // Pack groups into 128KB pages
            var currentGroupBatch = new List<int>();
            var currentBatchEstimatedBytes = sizeof(PageHeader);
            var batchLod = (byte)meshletData->groups[bulkGroupIndices[0]].lodLevel;

            for (var i = 0; i < bulkGroupIndices.Count; i++)
            {
                var gIdx = bulkGroupIndices[i];
                ref readonly var grp = ref meshletData->groups[gIdx];

                var groupBytes = EstimateGroupBytes(grp, meshletData);

                // If adding this group exceeds 128 KB, emit the current page batch
                if (currentGroupBatch.Count > 0 && (currentBatchEstimatedBytes + groupBytes > MeshletPageConstants.PAGE_SIZE))
                {
                    PackSinglePage(
                        pageIndex: (uint)pageDirectory.Count,
                        isRootPage: false,
                        groupIndices: CollectionsMarshal.AsSpan(currentGroupBatch),
                        predominantLod: batchLod,
                        meshletData: meshletData,
                        reorderedMeshlets: ref reorderedMeshlets,
                        pageDirectory: ref pageDirectory,
                        pageBuffers: pageBuffers,
                        handle: handle);

                    currentGroupBatch.Clear();
                    currentBatchEstimatedBytes = sizeof(PageHeader);
                    batchLod = (byte)grp.lodLevel;
                }

                currentGroupBatch.Add(gIdx);
                currentBatchEstimatedBytes += groupBytes;
            }

            if (currentGroupBatch.Count > 0)
            {
                PackSinglePage(
                    pageIndex: (uint)pageDirectory.Count,
                    isRootPage: false,
                    groupIndices: CollectionsMarshal.AsSpan(currentGroupBatch),
                    predominantLod: batchLod,
                    meshletData: meshletData,
                    reorderedMeshlets: ref reorderedMeshlets,
                    pageDirectory: ref pageDirectory,
                    pageBuffers: pageBuffers,
                    handle: handle);
            }
        }

        // Compute child page span for each group:
        // Groups at LOD 0 have no finer LOD, so childPageIndex = 0xFFFF, childPageCount = 0.
        // For groups at LOD L > 0, find all groups at LOD L - 1 whose bounding sphere intersects or overlaps this group.
        for (var g = 0; g < groupCount; g++)
        {
            ref var grp = ref meshletData->groups[g];
            if (grp.lodLevel == 0)
            {
                grp.childPageIndex = 0xFFFF;
                grp.childPageCount = 0;
            }
            else
            {
                var finerLod = grp.lodLevel - 1;
                var minChildPage = uint.MaxValue;
                var maxChildPage = 0u;
                var found = false;

                for (var p = 0; p < groupCount; p++)
                {
                    ref readonly var cand = ref meshletData->groups[p];
                    if (cand.lodLevel == finerLod)
                    {
                        var distSq = math.lengthsq(cand.boundingSphere.Center - grp.boundingSphere.Center);
                        var rSum = cand.boundingSphere.Radius + grp.boundingSphere.Radius;
                        if (distSq <= rSum * rSum)
                        {
                            if (cand.pageIndex < minChildPage) minChildPage = cand.pageIndex;
                            if (cand.pageIndex > maxChildPage) maxChildPage = cand.pageIndex;
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    var minDistanceSq = float.MaxValue;
                    var bestChildPage = 0xFFFFu;

                    for (var p = 0; p < groupCount; p++)
                    {
                        ref readonly var cand = ref meshletData->groups[p];
                        if (cand.lodLevel == finerLod)
                        {
                            var d = math.lengthsq(cand.boundingSphere.Center - grp.boundingSphere.Center);
                            if (d < minDistanceSq)
                            {
                                minDistanceSq = d;
                                bestChildPage = cand.pageIndex;
                            }
                        }
                    }

                    minChildPage = bestChildPage;
                    maxChildPage = bestChildPage;
                }

                if (minChildPage != 0xFFFFu && minChildPage != uint.MaxValue)
                {
                    grp.childPageIndex = (ushort)minChildPage;
                    grp.childPageCount = (ushort)(maxChildPage - minChildPage + 1u);
                }
                else
                {
                    grp.childPageIndex = 0xFFFF;
                    grp.childPageCount = 0;
                }
            }
        }

        // Update meshletData->meshlets with reordered meshlets
        meshletData->meshlets.Clear();
        meshletData->meshlets.AddRange(reorderedMeshlets.AsSpan());

        return new PackedMeshPages
        {
            reorderedMeshlets = reorderedMeshlets,
            pageDirectory = pageDirectory,
            pageBuffers = pageBuffers.ToArray(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EstimateGroupBytes(in MeshletGroup grp, MeshletMeshData* meshletData)
    {
        var bytes = (int)grp.meshletCount * sizeof(Meshlet);
        for (var m = 0; m < (int)grp.meshletCount; m++)
        {
            ref readonly var ml = ref meshletData->meshlets[(int)grp.meshletStartIndex + m];
            bytes += ml.vertexCount * sizeof(PackedVertex);
            bytes += ml.triangleCount * sizeof(uint);
        }
        return bytes;
    }

    private static void PackSinglePage(
        uint pageIndex,
        bool isRootPage,
        ReadOnlySpan<int> groupIndices,
        byte predominantLod,
        MeshletMeshData* meshletData,
        ref UnsafeList<Meshlet> reorderedMeshlets,
        ref UnsafeList<PageDirectoryEntry> pageDirectory,
        List<byte[]> pageBuffers,
        AllocationHandle handle)
    {
        var pageMeshletGlobalStart = (uint)reorderedMeshlets.Count;

        // Temporary buffers for the page components
        using var pageMeshlets = new UnsafeList<Meshlet>(64, handle);
        using var pageVertices = new UnsafeList<PackedVertex>(2048, handle);
        using var pageTriangles = new UnsafeList<uint>(4096, handle);

        for (var i = 0; i < groupIndices.Length; i++)
        {
            var gIdx = groupIndices[i];
            ref var grp = ref meshletData->groups[gIdx];

            var origMeshletStart = (int)grp.meshletStartIndex;
            var origMeshletCount = (int)grp.meshletCount;

            // Update group's page index and new global meshlet start index
            grp.pageIndex = pageIndex;
            grp.meshletStartIndex = pageMeshletGlobalStart + (uint)pageMeshlets.Count;

            for (var m = 0; m < origMeshletCount; m++)
            {
                ref readonly var srcMl = ref meshletData->meshlets[origMeshletStart + m];

                var localVertStart = (uint)pageVertices.Count;
                var localTriStart = (uint)pageTriangles.Count;

                // Copy vertices into page vertex stream
                for (var v = 0; v < srcMl.vertexCount; v++)
                {
                    pageVertices.Add(meshletData->meshletVertices[(int)srcMl.vertexOffset + v]);
                }

                // Copy micro-triangles into page triangle stream
                for (var t = 0; t < srcMl.triangleCount; t++)
                {
                    pageTriangles.Add(meshletData->meshletTriangles[(int)srcMl.triangleOffset + t]);
                }

                // Create meshlet copy with local offsets to be patched below
                var dstMl = srcMl;
                dstMl.pageIndex = pageIndex;
                dstMl.vertexOffset = localVertStart;      // temporarily hold local vertex start
                dstMl.triangleOffset = localTriStart;    // temporarily hold local triangle start

                pageMeshlets.Add(dstMl);
            }
        }

        // Layout inside page:
        // [0 .. 32)                       : PageHeader (32 bytes)
        // [32 .. vertOffset)              : Meshlet[]
        // [vertOffset .. triOffset)       : PackedVertex[]
        // [triOffset .. totalBytes)       : uint[] micro-triangles
        var headerSize = (uint)sizeof(PageHeader);
        var meshletsSize = (uint)(pageMeshlets.Count * sizeof(Meshlet));
        var vertOffset = headerSize + meshletsSize;
        var verticesSize = (uint)(pageVertices.Count * sizeof(PackedVertex));
        var triOffset = vertOffset + verticesSize;
        var trianglesSize = (uint)(pageTriangles.Count * sizeof(uint));
        var totalPayloadBytes = triOffset + trianglesSize;

        // Patch each meshlet with byte offsets from page start
        for (var m = 0; m < pageMeshlets.Count; m++)
        {
            ref var ml = ref pageMeshlets[m];
            var localVertStart = ml.vertexOffset;
            var localTriStart = ml.triangleOffset;

            ml.vertexOffset = vertOffset + localVertStart * (uint)sizeof(PackedVertex);
            ml.triangleOffset = triOffset + localTriStart * sizeof(uint);

            // Add to the global reordered meshlet array
            reorderedMeshlets.Add(ml);
        }

        // Create page buffer
        var pageBuffer = new byte[totalPayloadBytes];
        fixed (byte* pDst = pageBuffer)
        {
            // 1. Write PageHeader
            var pHeader = (PageHeader*)pDst;
            pHeader->magic = PageHeader.MAGIC;
            pHeader->meshletCount = (ushort)pageMeshlets.Count;
            pHeader->vertexCount = (ushort)pageVertices.Count;
            pHeader->triangleCount = (uint)pageTriangles.Count;
            pHeader->meshletOffset = headerSize;
            pHeader->vertexOffset = vertOffset;
            pHeader->triangleOffset = triOffset;
            pHeader->pageIndex = pageIndex;
            pHeader->reserved = 0;

            // 2. Write Meshlets
            if (pageMeshlets.Count > 0)
            {
                fixed (Meshlet* pMl = pageMeshlets.AsSpan())
                {
                    Buffer.MemoryCopy(pMl, pDst + headerSize, meshletsSize, meshletsSize);
                }
            }

            // 3. Write PackedVertices
            if (pageVertices.Count > 0)
            {
                fixed (PackedVertex* pVerts = pageVertices.AsSpan())
                {
                    Buffer.MemoryCopy(pVerts, pDst + vertOffset, verticesSize, verticesSize);
                }
            }

            // 4. Write Micro-triangles
            if (pageTriangles.Count > 0)
            {
                fixed (uint* pTris = pageTriangles.AsSpan())
                {
                    Buffer.MemoryCopy(pTris, pDst + triOffset, trianglesSize, trianglesSize);
                }
            }
        }

        // Create directory entry
        var dirEntry = new PageDirectoryEntry
        {
            fileOffset = 0, // patched when writing to file
            compressedSize = (uint)totalPayloadBytes,
            uncompressedSize = (uint)totalPayloadBytes,
            meshletStartIndex = pageMeshletGlobalStart,
            meshletCount = (ushort)pageMeshlets.Count,
            lodLevel = predominantLod,
            isRootPage = (byte)(isRootPage ? 1 : 0),
            reserved0 = 0,
            reserved1 = 0,
        };

        pageDirectory.Add(dirEntry);
        pageBuffers.Add(pageBuffer);
    }
}
