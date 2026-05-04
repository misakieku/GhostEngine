using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine;

[StructLayout(LayoutKind.Sequential)]
public struct MeshContentHeader
{
    public const uint MAGIC = 0x48534D47; // GMSH
    public const uint VERSION = 1;

    public uint magic;
    public uint version;

    public uint vertexCount;
    public uint indexCount;
    public uint materialPartCount;
    public uint meshletCount;
    public uint meshletGroupCount;
    public uint meshletHierarchyNodeCount;
    public uint meshletVertexCount;
    public uint meshletTriangleCount;
    public uint materialSlotCount;
    public uint lodLevelCount;

    public float3 boundsMin;
    public float3 boundsMax;

    public ulong vertexOffset;
    public ulong indexOffset;
    public ulong materialPartOffset;
    public ulong meshletOffset;
    public ulong meshletGroupOffset;
    public ulong meshletHierarchyNodeOffset;
    public ulong meshletVertexOffset;
    public ulong meshletTriangleOffset;
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
