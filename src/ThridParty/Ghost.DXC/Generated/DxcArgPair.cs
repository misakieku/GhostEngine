namespace Ghost.DXC;

/// <include file='DxcArgPair.xml' path='doc/member[@name="DxcArgPair"]/*' />
public unsafe partial struct DxcArgPair
{
    /// <include file='DxcArgPair.xml' path='doc/member[@name="DxcArgPair.pName"]/*' />
    [NativeTypeName("const WCHAR *")]
    public char* pName;

    /// <include file='DxcArgPair.xml' path='doc/member[@name="DxcArgPair.pValue"]/*' />
    [NativeTypeName("const WCHAR *")]
    public char* pValue;
}
