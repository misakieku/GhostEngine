using Misaki.HighPerformance.Mathematics;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
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

[StructLayout(LayoutKind.Sequential)]
public struct Meshlet
{
    public SphereBounds boundingSphere;         // 16 bytes
    public SphereBounds parentBoundingSphere;   // 16 bytes
    public AABB boundingBox;                    // 24 bytes
    public uint vertexOffset;                   // offset into meshlet vertex index array
    public uint triangleOffset;                 // offset into packed triangle array
    public uint groupIndex;                     // owning group
    public float clusterError;                  // geometric error of this meshlet/cluster
    public float parentError;                   // geometric refinement error carried into runtime LOD tests
    public byte vertexCount;                    // max 64
    public byte triangleCount;                  // max 124
    public byte localMaterialIndex;             // mesh-local material slot
    public byte lodLevel;                       // this meshlet's LOD level
}

[StructLayout(LayoutKind.Sequential)]
public struct MeshletGroup
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public float parentError;             // error of refining to the previous level
    public uint meshletStartIndex;        // contiguous meshlet range
    public uint meshletCount;             // number of meshlets in the group
    public uint lodLevel;                 // group LOD level
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
public struct MeshletHierarchyNode
{
    /// <summary> Bounding sphere (center.xyz, radius) in object space. </summary>
    public SphereBounds bounds;
    /// <summary> Conservative simplification error of the subtree in object space. </summary>
    public float error;
    /// <summary> Leaf node: index of the MeshletGroup this node represents; Internal node: -1. </summary>
    public int groupIndex;
    /// <summary> Offset to the first child node in the hierarchy array. </summary>
    public uint childOffset;
    /// <summary> Number of contiguous child nodes (0 for leaves). </summary>
    public uint childCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct MeshletMeshData : IDisposable
{
    public UnsafeList<Meshlet> meshlets;
    public UnsafeList<MeshletGroup> groups;
    public UnsafeList<MeshletHierarchyNode> hierarchyNodes;
    public UnsafeList<uint> meshletVertices;
    public UnsafeList<uint> meshletTriangles;
    public int meshletCount;
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

[StructLayout(LayoutKind.Sequential, Size = 192)]
public struct MeshContentHeader
{
    public const uint MAGIC = 0x48534D47; // GMSH
    public const uint VERSION = 1;

    public uint magic;
    public uint version;

    public int vertexCount;
    public int indexCount;
    public int materialPartCount;
    public int meshletCount;
    public int meshletGroupCount;
    public int meshletHierarchyNodeCount;
    public int meshletVertexCount;
    public int meshletTriangleCount;
    public int materialSlotCount;
    public int lodLevelCount;

    public float3 boundsMin;
    public float3 boundsMax;

    public long vertexOffset;
    public long indexOffset;
    public long materialPartOffset;
    public long meshletOffset;
    public long meshletGroupOffset;
    public long meshletHierarchyNodeOffset;
    public long meshletVertexOffset;
    public long meshletTriangleOffset;
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
