using System.Runtime.InteropServices;

namespace Ghost.Core;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MaterialContentHeader
{
    public const uint MAGIC = 0x54414D47; // 'GMAT'
    public const uint VERSION = 1;

    public uint magic;
    public uint version;
    public Guid shaderId;
    public uint propertyBufferSize;
    public uint textureFixupCount;
    public long propertyBufferOffset;
    public long textureFixupOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MaterialTextureFixup
{
    public uint byteOffset;
    public Guid textureGuid;
}
