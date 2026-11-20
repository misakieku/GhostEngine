using System.Runtime.InteropServices;

namespace Ghost.Core;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MaterialContentHeader
{
    public const uint MAGIC = 0x54414D47; // 'GMAT'
    public const uint VERSION = 2;

    public uint magic;
    public uint version;
    public Guid shaderId;
    public uint propertyBufferSize;
    public uint textureFixupCount;
    public uint samplerFixupCount;
    public long propertyBufferOffset;
    public long textureFixupOffset;
    public long samplerFixupOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MaterialTextureFixup
{
    public uint byteOffset;
    public Guid textureGuid;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MaterialSamplerFixup
{
    public uint byteOffset;
    public StaticSamplerType samplerType;
}
