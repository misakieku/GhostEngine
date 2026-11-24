namespace Ghost.D3D12;

public unsafe partial struct _D3D_SHADER_MACRO
{
    [NativeTypeName("LPCSTR")]
    public sbyte* Name;

    [NativeTypeName("LPCSTR")]
    public sbyte* Definition;
}
