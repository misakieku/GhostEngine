using System.Runtime.CompilerServices;

namespace Ghost.DXC;

public partial struct DxcShaderHash
{
    [NativeTypeName("UINT32")]
    public uint Flags;

    [NativeTypeName("BYTE[16]")]
    public _HashDigest_e__FixedBuffer HashDigest;

    [InlineArray(16)]
    public partial struct _HashDigest_e__FixedBuffer
    {
        public byte e0;
    }
}
