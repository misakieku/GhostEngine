using System.Runtime.InteropServices;

namespace Ghost.Core;

public enum AssetType
{
    Texture = 0,
    Mesh = 1,
    Material = 2,
    Shader = 3,
    Scene = 4,
    Audio = 5,
    Video = 6,
    Json = 7,

    Unknown = 64,
}

public enum CompressionMethod
{
    None,
    Zstd,
    LZ4
}

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct AssetHeader()
{
    public const uint MAGIC = 0x47415353; // "SSAG" in little-endian
    public const uint VERSION = 1;
    
    public uint magic = MAGIC;
    public uint version = VERSION;
    
    public AssetType assetType;
    public ulong size; // Size of the asset data after the header, in bytes. This is the size of the compressed data if compression is used.
}

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct TextureContentHeader()
{
    public const uint MAGIC = 0x58455447; // GTEX
    public const uint VERSION = 1;

    public uint magic = MAGIC;
    public uint version = VERSION;

    public uint width;
    public uint height;
    public uint bpc;
    public uint mipLevels;
    public uint dimension; // 1 for 1D, 2 for 2D, 3 for 3D, 4 for Cube, etc. See TextureDimension
    public uint colorComponents;
}