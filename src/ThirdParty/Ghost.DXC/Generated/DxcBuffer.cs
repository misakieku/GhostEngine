namespace Ghost.DXC;

/// <include file='DxcBuffer.xml' path='doc/member[@name="DxcBuffer"]/*' />
public unsafe partial struct DxcBuffer
{
    /// <include file='DxcBuffer.xml' path='doc/member[@name="DxcBuffer.Ptr"]/*' />
    [NativeTypeName("LPCVOID")]
    public void* Ptr;

    /// <include file='DxcBuffer.xml' path='doc/member[@name="DxcBuffer.Size"]/*' />
    [NativeTypeName("SIZE_T")]
    public ulong Size;

    /// <include file='DxcBuffer.xml' path='doc/member[@name="DxcBuffer.Encoding"]/*' />
    public uint Encoding;
}
