using System.Runtime.InteropServices;

namespace Ghost.AssetBaker.Bakers;

public enum TextureDimension : uint
{
    Unknown = unchecked((uint)-1),
    None = 0,
    Texture1D = 1,
    Texture2D = 2,
    Texture3D = 3,
    TextureCube = 4,
    Texture2DArray = 5,
    TextureCubeArray = 6
}


[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct TextureContentHeader
{
    public const uint MAGIC = 0x58455447; // GTEX
    public const uint VERSION = 1;

    public uint magic;
    public uint version;

    public uint width;
    public uint height;
    public uint bpc;
    public uint mipLevels;
    public uint dimension; // 1 for 1D, 2 for 2D, 3 for 3D, 4 for Cube, etc. See TextureDimension
    public uint colorComponents;
}
