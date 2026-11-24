namespace Ghost.D3D12;

public unsafe partial struct D3D12_PARTIAL_GRAPHICS_PROGRAM_DESC
{
    [NativeTypeName("LPCWSTR")]
    public char* ProgramName;

    public uint NumExports;

    [NativeTypeName("LPCWSTR *")]
    public char** pExports;

    public uint NumSubobjects;

    [NativeTypeName("const D3D12_STATE_SUBOBJECT *const *")]
    public D3D12_STATE_SUBOBJECT** ppSubobjects;

    public D3D12_PARTIAL_GRAPHICS_PROGRAM_TYPE ProgramType;
}
