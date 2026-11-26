namespace Ghost.DXC;

public unsafe partial struct DxcDefine
{
    [NativeTypeName("LPCWSTR")]
    public char* Name;

    [NativeTypeName("LPCWSTR")]
    public char* Value;
}
