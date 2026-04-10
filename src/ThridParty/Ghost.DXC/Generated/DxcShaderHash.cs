using System.Runtime.CompilerServices;

namespace Ghost.DXC;

/// <include file='DxcShaderHash.xml' path='doc/member[@name="DxcShaderHash"]/*' />
public partial struct DxcShaderHash
{
    /// <include file='DxcShaderHash.xml' path='doc/member[@name="DxcShaderHash.Flags"]/*' />
    [NativeTypeName("UINT32")]
    public uint Flags;

    /// <include file='DxcShaderHash.xml' path='doc/member[@name="DxcShaderHash.HashDigest"]/*' />
    [NativeTypeName("BYTE[16]")]
    public _HashDigest_e__FixedBuffer HashDigest;

    /// <include file='_HashDigest_e__FixedBuffer.xml' path='doc/member[@name="_HashDigest_e__FixedBuffer"]/*' />
    [InlineArray(16)]
    public partial struct _HashDigest_e__FixedBuffer
    {
        public byte e0;
    }
}
