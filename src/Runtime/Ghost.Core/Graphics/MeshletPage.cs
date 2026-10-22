using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

public static class MeshletPageConstants
{
    /// <summary>
    /// Fixed page size for meshlet streaming pages (128 KB).
    /// </summary>
    public const int PAGE_SIZE = 128 * 1024; // 131,072 bytes
}

/// <summary>
/// Fixed-size header stored at the beginning of each 128KB meshlet page.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 32)]
public struct PageHeader
{
    public const uint MAGIC = 0x50414745; // 'PAGE'

    public uint magic;                  // 4 bytes
    public ushort meshletCount;         // 2 bytes
    public ushort vertexCount;          // 2 bytes
    public uint triangleCount;          // 4 bytes
    public uint meshletOffset;          // 4 bytes (byte offset from page start to Meshlet[])
    public uint vertexOffset;           // 4 bytes (byte offset from page start to PackedVertex[])
    public uint triangleOffset;         // 4 bytes (byte offset from page start to uint[] micro-triangles)
    public uint pageIndex;              // 4 bytes (logical page index within the asset)
    public uint reserved;               // 4 bytes (alignment padding)
}

/// <summary>
/// Directory entry stored in .gmsh v3 assets describing the location and payload size of each 128KB page.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 32)]
public struct PageDirectoryEntry
{
    public ulong fileOffset;            // 8 bytes (byte offset within .gmsh file)
    public uint compressedSize;         // 4 bytes (size on disk)
    public uint uncompressedSize;       // 4 bytes (actual payload size in bytes, <= 131072)
    public uint meshletStartIndex;      // 4 bytes (first global meshlet index in this page)
    public ushort meshletCount;         // 2 bytes (number of meshlets in this page)
    public byte lodLevel;               // 1 byte (predominant LOD level)
    public byte isRootPage;             // 1 byte (1 if Root Page 0, 0 for bulk streaming pages)
    public uint reserved0;              // 4 bytes
    public uint reserved1;              // 4 bytes
}
