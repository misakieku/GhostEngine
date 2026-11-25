using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct MaterialPartInfo
{
    public int materialIndex;
    public int vertexStart;
    public int vertexCount;
    public int indexStart;
    public int indexCount;
}

[StructLayout(LayoutKind.Sequential, Size = 96)]
public struct Meshlet
{
    public SphereBounds boundingSphere;         // 16 bytes
    public SphereBounds parentBoundingSphere;   // 16 bytes
    public AABB boundingBox;                    // 24 bytes
    public uint vertexOffset;                   // 4 bytes (byte offset from page start to quantized position bitstream)
    public uint triangleOffset;                 // 4 bytes (byte offset from page start to 18-bit packed triangle bitstream)
    public uint pageIndex;                      // 4 bytes (owning page index)
    public float clusterError;                  // 4 bytes (geometric error of this meshlet/cluster)
    public float parentError;                   // 4 bytes (geometric refinement error carried into runtime LOD tests)
    public byte vertexCount;                    // 1 byte (max 64)
    public byte triangleCount;                  // 1 byte (max 126)
    public byte localMaterialIndex;             // 1 byte (mesh-local material slot)
    public byte lodLevel;                       // 1 byte (this meshlet's LOD level)
    public float maxEdgeLength;                 // 4 bytes (maximum triangle edge length in object space)
    public uint packedQuantOrigin0;             // 4 bytes (qMin.x [21b] | qMin.y_low [11b])
    public uint packedQuantOrigin1;             // 4 bytes (qMin.y_high [10b] | qMin.z [21b])
    public uint packedFormat;                   // 4 bytes (bitsX [5b] | bitsY [5b] | bitsZ [5b] | hasColor [1b] | attrWordOffset [15b])
}

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct MeshletGroup
{
    public SphereBounds boundingSphere;         // 16 bytes
    public AABB boundingBox;                    // 24 bytes
    public float parentError;                   // 4 bytes
    public uint meshletStartIndex;              // 4 bytes (contiguous meshlet range)
    public uint meshletCount;                   // 4 bytes (number of meshlets in the group)
    public uint pageIndex;                      // 4 bytes (page owning this group)
    public ushort childPageIndex;               // 2 bytes (finer LOD child starting page index, 0xFFFF if finest/LOD 0)
    public ushort childPageCount;               // 2 bytes (number of finer LOD child pages, 0 if finest/LOD 0)
    public uint lodLevel;                       // 4 bytes (group LOD level)

    public readonly uint PackedChildPageRange => childPageIndex | ((uint)childPageCount << 16);
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
public struct MeshletHierarchyNode
{
    public SphereBounds bounds;
    public float error;
    public int groupIndex;
    public uint childOffset;
    public uint childCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct MeshletMeshData : IDisposable
{
    public UnsafeList<Meshlet> meshlets;
    public UnsafeList<MeshletGroup> groups;
    public UnsafeList<MeshletHierarchyNode> hierarchyNodes;
    public UnsafeList<PackedVertex> meshletVertices;
    public UnsafeList<uint> meshletTriangles;
    public int meshletCount;
    public int meshletGroupCount;
    public int lodLevelCount;
    public int materialSlotCount;

    public readonly MeshletMeshData Clone()
    {
        var newData = this;

        newData.meshlets = meshlets.Clone(AllocationHandle.Persistent);
        newData.groups = groups.Clone(AllocationHandle.Persistent);
        newData.hierarchyNodes = hierarchyNodes.Clone(AllocationHandle.Persistent);
        newData.meshletVertices = meshletVertices.Clone(AllocationHandle.Persistent);
        newData.meshletTriangles = meshletTriangles.Clone(AllocationHandle.Persistent);

        return newData;
    }

    public void Dispose()
    {
        meshlets.Dispose();
        groups.Dispose();
        hierarchyNodes.Dispose();
        meshletVertices.Dispose();
        meshletTriangles.Dispose();
    }
}

[StructLayout(LayoutKind.Sequential, Size = 256)]
public struct MeshContentHeader
{
    public const uint MAGIC = 0x48534D47; // GMSH
    public const uint VERSION = 6;

    public const byte METADATA_MASK_HIERARCHY = 1 << 0;
    public const byte METADATA_MASK_GROUP = 1 << 1;
    public const byte METADATA_MASK_MESHLET = 1 << 2;
    public const byte METADATA_MASK_RAYTRACING_PROXY = 1 << 3;

    public uint magic;
    public uint version;

    public int meshletCount;
    public int meshletGroupCount;
    public int meshletHierarchyNodeCount;
    public int materialSlotCount;
    public int lodLevelCount;
    public int pageCount;
    public int rootPageCount;
    public int rootMeshletCount;

    public float3 boundsMin;
    public float3 boundsMax;
    public float3 boundingSphereCenter;
    public float boundingSphereRadius;

    public long hierarchyOffset;
    public long hierarchySize;
    public long groupOffset;
    public long groupSize;
    public long meshletOffset;
    public long meshletSize;
    public long pageDirectoryOffset;
    public long pageDirectorySize;
    public long rootPageOffset;
    public long rootPageSize;
    public long bulkPagesOffset;
    public long bulkPagesSize;
    public long materialPartOffset;
    public long materialPartSize;

    public long rayTracingProxyOffset;
    public long rayTracingProxySize;
    public int rayTracingProxyVertexCount;
    public int rayTracingProxyIndexCount;

    public long hierarchyUncompressedSize;
    public long groupUncompressedSize;
    public long meshletUncompressedSize;
    public long rayTracingProxyUncompressedSize;
    public PageCompressionMethod metadataCompression;
    public byte metadataCompressionMask;
    private ushort _reserved0;
    public float positionQuantStep;
}

[StructLayout(LayoutKind.Sequential)]
public struct MeshContentMaterialPart
{
    public int materialIndex;
    public int indexStart;
    public int indexCount;
    public int vertexStart;
    public int vertexCount;
}
