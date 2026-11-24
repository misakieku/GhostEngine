using Ghost.Core.Graphics;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
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
    public float positionQuantStep;

    public void Dispose()
    {
        reorderedMeshlets.Dispose();
        pageDirectory.Dispose();
    }
}

/// <summary>
/// Bundles cluster meshlets, quantized local positions, compact attributes, and 18-bit micro-triangles into 128KB self-contained pages.
/// </summary>
public static unsafe class PagePacker
{
    private struct MeshletEncodingInfo
    {
        public uint minX;
        public uint minY;
        public uint minZ;
        public int bitsX;
        public int bitsY;
        public int bitsZ;
        public bool hasVertexColor;
        public int posByteSize;
        public int attrByteSize;
        public int triByteSize;

        public readonly int TotalBytes => posByteSize + attrByteSize + triByteSize;
    }

    public static PackedMeshPages PackPages(MeshletMeshData* meshletData, AABB meshBounds, int positionQuantBits, AllocationHandle handle)
    {
        var totalMeshletCount = meshletData->meshlets.Count;
        var groupCount = meshletData->groups.Count;
        var levelCount = meshletData->lodLevelCount;

        var clampedBits = Math.Clamp(positionQuantBits, 16, PackedVertex.MAX_POSITION_QUANT_BITS);
        var maxGrid = (1u << clampedBits) - 1u;
        var quantStep = PackedVertex.ComputePositionQuantStep(meshBounds.Min, meshBounds.Max, clampedBits);

        using var encodingInfos = new UnsafeArray<MeshletEncodingInfo>(Math.Max(1, totalMeshletCount), handle);
        PrecomputeMeshletEncodingInfos(meshletData, meshBounds.Min, quantStep, maxGrid, encodingInfos.AsSpan());

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
                    lodBytes += EstimateGroupBytes(meshletData->groups[g], encodingInfos.AsReadOnly());
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

        // Pack Page 0: Root Page (Pinned coarsest LOD)
        PackSinglePage(
            pageIndex: 0,
            isRootPage: true,
            groupIndices: rootGroupIndices.AsSpan(),
            predominantLod: (byte)rootLod,
            meshletData: meshletData,
            encodingInfos: encodingInfos.AsReadOnly(),
            boundsMin: meshBounds.Min,
            quantStep: quantStep,
            maxGrid: maxGrid,
            reorderedMeshlets: ref reorderedMeshlets,
            pageDirectory: ref pageDirectory,
            pageBuffers: pageBuffers,
            handle: handle);

        // Pack Bulk Pages 1..N (Streamable fine LODs)
        if (bulkGroupIndices.Count > 0)
        {
            var currentGroupBatch = new List<int>();
            var currentBatchEstimatedBytes = sizeof(PageHeader);
            var batchLod = (byte)meshletData->groups[bulkGroupIndices[0]].lodLevel;

            for (var i = 0; i < bulkGroupIndices.Count; i++)
            {
                var gIdx = bulkGroupIndices[i];
                ref readonly var grp = ref meshletData->groups[gIdx];

                var groupBytes = EstimateGroupBytes(grp, encodingInfos.AsReadOnly());

                // If adding this group exceeds 128 KB, emit the current page batch
                if (currentGroupBatch.Count > 0 && (currentBatchEstimatedBytes + groupBytes > MeshletPageConstants.PAGE_SIZE))
                {
                    PackSinglePage(
                        pageIndex: (uint)pageDirectory.Count,
                        isRootPage: false,
                        groupIndices: CollectionsMarshal.AsSpan(currentGroupBatch),
                        predominantLod: batchLod,
                        meshletData: meshletData,
                        encodingInfos: encodingInfos.AsReadOnly(),
                        boundsMin: meshBounds.Min,
                        quantStep: quantStep,
                        maxGrid: maxGrid,
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
                    encodingInfos: encodingInfos.AsReadOnly(),
                    boundsMin: meshBounds.Min,
                    quantStep: quantStep,
                    maxGrid: maxGrid,
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
            positionQuantStep = quantStep,
        };
    }

    private static void PrecomputeMeshletEncodingInfos(MeshletMeshData* meshletData, float3 boundsMin, float quantStep, uint maxGrid, Span<MeshletEncodingInfo> infos)
    {
        for (var m = 0; m < meshletData->meshlets.Count; m++)
        {
            ref readonly var ml = ref meshletData->meshlets[m];
            var minX = uint.MaxValue;
            var minY = uint.MaxValue;
            var minZ = uint.MaxValue;
            var maxX = 0u;
            var maxY = 0u;
            var maxZ = 0u;
            var hasColor = false;

            for (var v = 0; v < ml.vertexCount; v++)
            {
                ref readonly var vert = ref meshletData->meshletVertices[(int)ml.vertexOffset + v];
                PackedVertex.QuantizePosition(vert.position, boundsMin, quantStep, maxGrid, out var qx, out var qy, out var qz);
                if (qx < minX) minX = qx;
                if (qy < minY) minY = qy;
                if (qz < minZ) minZ = qz;
                if (qx > maxX) maxX = qx;
                if (qy > maxY) maxY = qy;
                if (qz > maxZ) maxZ = qz;

                if (vert.color.r != 255 || vert.color.g != 255 || vert.color.b != 255 || vert.color.a != 255)
                {
                    hasColor = true;
                }
            }

            if (ml.vertexCount == 0)
            {
                minX = 0;
                minY = 0;
                minZ = 0;
            }

            var bitsX = PackedVertex.BitWidth(maxX - minX);
            var bitsY = PackedVertex.BitWidth(maxY - minY);
            var bitsZ = PackedVertex.BitWidth(maxZ - minZ);
            var totalPosBits = ml.vertexCount * (bitsX + bitsY + bitsZ);
            var posWords = totalPosBits > 0 ? ((totalPosBits + 31) >> 5) : 0;
            var attrStride = hasColor ? 12 : 8;
            var triBits = ml.triangleCount * 18;
            var triWords = triBits > 0 ? ((triBits + 31) >> 5) : 0;

            infos[m] = new MeshletEncodingInfo
            {
                minX = minX,
                minY = minY,
                minZ = minZ,
                bitsX = bitsX,
                bitsY = bitsY,
                bitsZ = bitsZ,
                hasVertexColor = hasColor,
                posByteSize = posWords * 4,
                attrByteSize = ml.vertexCount * attrStride,
                triByteSize = triWords * 4,
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EstimateGroupBytes(in MeshletGroup grp, ReadOnlyView<MeshletEncodingInfo> encodingInfos)
    {
        var bytes = 0;
        for (var m = 0; m < (int)grp.meshletCount; m++)
        {
            bytes += encodingInfos[(int)grp.meshletStartIndex + m].TotalBytes;
        }
        return bytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteBits(uint* pDst, int bitOffset, uint value, int bitCount)
    {
        if (bitCount <= 0)
        {
            return;
        }

        var wordIndex = bitOffset >> 5;
        var bitInWord = bitOffset & 31;
        var masked = value & ((1u << bitCount) - 1u);
        pDst[wordIndex] |= masked << bitInWord;
        if (bitInWord + bitCount > 32)
        {
            pDst[wordIndex + 1] |= masked >> (32 - bitInWord);
        }
    }

    private static void PackSinglePage(uint pageIndex, bool isRootPage, ReadOnlySpan<int> groupIndices, byte predominantLod, MeshletMeshData* meshletData, ReadOnlyView<MeshletEncodingInfo> encodingInfos, float3 boundsMin, float quantStep, uint maxGrid, ref UnsafeList<Meshlet> reorderedMeshlets, ref UnsafeList<PageDirectoryEntry> pageDirectory, List<byte[]> pageBuffers, AllocationHandle handle)
    {
        var pageMeshletGlobalStart = (uint)reorderedMeshlets.Count;

        using var pageMeshlets = new UnsafeList<Meshlet>(64, handle);
        using var pageOrigMeshletIndices = new UnsafeList<int>(64, handle);

        var totalPosBytes = 0u;
        var totalAttrBytes = 0u;
        var totalTriBytes = 0u;
        var totalVertexCount = 0u;
        var totalTriangleCount = 0u;

        for (var i = 0; i < groupIndices.Length; i++)
        {
            var gIdx = groupIndices[i];
            ref var grp = ref meshletData->groups[gIdx];

            var origMeshletStart = (int)grp.meshletStartIndex;
            var origMeshletCount = (int)grp.meshletCount;

            grp.pageIndex = pageIndex;
            grp.meshletStartIndex = pageMeshletGlobalStart + (uint)pageMeshlets.Count;

            for (var m = 0; m < origMeshletCount; m++)
            {
                var origIdx = origMeshletStart + m;
                ref readonly var srcMl = ref meshletData->meshlets[origIdx];
                ref readonly var info = ref encodingInfos[origIdx];

                var dstMl = srcMl;
                dstMl.pageIndex = pageIndex;
                dstMl.vertexOffset = totalPosBytes;
                dstMl.triangleOffset = totalTriBytes;
                PackedVertex.PackQuantOrigin(info.minX, info.minY, info.minZ, out dstMl.packedQuantOrigin0, out dstMl.packedQuantOrigin1);
                dstMl.packedFormat = PackedVertex.PackMeshletFormat(info.bitsX, info.bitsY, info.bitsZ, info.hasVertexColor, totalAttrBytes);

                pageMeshlets.Add(dstMl);
                pageOrigMeshletIndices.Add(origIdx);

                totalPosBytes += (uint)info.posByteSize;
                totalAttrBytes += (uint)info.attrByteSize;
                totalTriBytes += (uint)info.triByteSize;
                totalVertexCount += srcMl.vertexCount;
                totalTriangleCount += srcMl.triangleCount;
            }
        }

        // Layout inside page (all sections 4-byte aligned):
        // [0 .. 32)                  : PageHeader (32 bytes)
        // [posOffset .. attrOffset)  : Quantized position bitstreams
        // [attrOffset .. triOffset)  : PackedAttribute (+ optional Color32) streams
        // [triOffset .. totalBytes)  : 18-bit (6+6+6) triangle index bitstreams
        var headerSize = (uint)sizeof(PageHeader);
        var posOffset = headerSize;
        var attrOffset = posOffset + totalPosBytes;
        var triOffset = attrOffset + totalAttrBytes;
        var totalPayloadBytes = triOffset + totalTriBytes;

        // Patch meshlets with final page-relative byte offsets
        for (var m = 0; m < pageMeshlets.Count; m++)
        {
            ref var ml = ref pageMeshlets[m];
            ref readonly var info = ref encodingInfos[pageOrigMeshletIndices[m]];
            var relAttrOffset = (ml.packedFormat >> 16) << 2;

            ml.vertexOffset = posOffset + ml.vertexOffset;
            ml.triangleOffset = triOffset + ml.triangleOffset;
            ml.packedFormat = PackedVertex.PackMeshletFormat(info.bitsX, info.bitsY, info.bitsZ, info.hasVertexColor, attrOffset + relAttrOffset);

            reorderedMeshlets.Add(ml);
        }

        var pageBuffer = new byte[totalPayloadBytes];
        fixed (byte* pDst = pageBuffer)
        {
            var pHeader = (PageHeader*)pDst;
            pHeader->magic = PageHeader.MAGIC;
            pHeader->meshletCount = (ushort)pageMeshlets.Count;
            pHeader->vertexCount = (ushort)totalVertexCount;
            pHeader->triangleCount = totalTriangleCount;
            pHeader->meshletOffset = 0;
            pHeader->vertexOffset = posOffset;
            pHeader->triangleOffset = triOffset;
            pHeader->pageIndex = pageIndex;
            pHeader->reserved = attrOffset;

            for (var m = 0; m < pageMeshlets.Count; m++)
            {
                var origIdx = pageOrigMeshletIndices[m];
                ref readonly var srcMl = ref meshletData->meshlets[origIdx];
                ref readonly var dstMl = ref pageMeshlets[m];
                ref readonly var info = ref encodingInfos[origIdx];

                var pPosDst = (uint*)(pDst + dstMl.vertexOffset);
                var attrByteOffset = (dstMl.packedFormat >> 16) << 2;
                var pAttrDst = pDst + attrByteOffset;
                var pTriDst = (uint*)(pDst + dstMl.triangleOffset);

                var totalPosBits = info.bitsX + info.bitsY + info.bitsZ;
                var attrStride = info.hasVertexColor ? 12 : 8;

                for (var v = 0; v < srcMl.vertexCount; v++)
                {
                    ref readonly var vert = ref meshletData->meshletVertices[(int)srcMl.vertexOffset + v];

                    if (totalPosBits > 0)
                    {
                        PackedVertex.QuantizePosition(vert.position, boundsMin, quantStep, maxGrid, out var qx, out var qy, out var qz);
                        var baseBit = v * totalPosBits;
                        WriteBits(pPosDst, baseBit, qx - info.minX, info.bitsX);
                        WriteBits(pPosDst, baseBit + info.bitsX, qy - info.minY, info.bitsY);
                        WriteBits(pPosDst, baseBit + info.bitsX + info.bitsY, qz - info.minZ, info.bitsZ);
                    }

                    var pAttr = (PackedAttribute*)(pAttrDst + v * attrStride);
                    pAttr->packedNormal = vert.packedNormal;
                    pAttr->uv = vert.uv;
                    if (info.hasVertexColor)
                    {
                        *(Color32*)(pAttrDst + v * attrStride + sizeof(PackedAttribute)) = vert.color;
                    }
                }

                for (var t = 0; t < srcMl.triangleCount; t++)
                {
                    var packedTri = meshletData->meshletTriangles[(int)srcMl.triangleOffset + t];
                    var i0 = packedTri & 0xFFu;
                    var i1 = (packedTri >> 8) & 0xFFu;
                    var i2 = (packedTri >> 16) & 0xFFu;
                    WriteBits(pTriDst, t * 18, i0 | (i1 << 6) | (i2 << 12), 18);
                }
            }
        }

        var dirEntry = new PageDirectoryEntry
        {
            fileOffset = 0,
            compressedSize = totalPayloadBytes,
            uncompressedSize = totalPayloadBytes,
            meshletStartIndex = pageMeshletGlobalStart,
            meshletCount = (ushort)pageMeshlets.Count,
            lodLevel = predominantLod,
            isRootPage = (byte)(isRootPage ? 1 : 0),
            compressionMethod = PageCompressionMethod.None,
            reserved0 = 0,
            reserved1 = 0,
            reserved2 = 0,
        };

        pageDirectory.Add(dirEntry);
        pageBuffers.Add(pageBuffer);
    }
}
