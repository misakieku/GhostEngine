namespace Ghost.DXC;

/// <include file='DxcDefine.xml' path='doc/member[@name="DxcDefine"]/*' />
public unsafe partial struct DxcDefine
{
    /// <include file='DxcDefine.xml' path='doc/member[@name="DxcDefine.Name"]/*' />
    [NativeTypeName("LPCWSTR")]
    public char* Name;

    /// <include file='DxcDefine.xml' path='doc/member[@name="DxcDefine.Value"]/*' />
    [NativeTypeName("LPCWSTR")]
    public char* Value;
}
