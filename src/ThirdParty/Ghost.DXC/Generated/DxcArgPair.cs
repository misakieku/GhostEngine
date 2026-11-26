namespace Ghost.DXC;

public unsafe partial struct DxcArgPair
{
    [NativeTypeName("const WCHAR *")]
    public char* pName;

    [NativeTypeName("const WCHAR *")]
    public char* pValue;
}
