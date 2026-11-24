namespace Ghost.D3D12;

public unsafe partial struct D3D12_EXPORT_DESC
{
    [NativeTypeName("LPCWSTR")]
    public char* Name;

    [NativeTypeName("LPCWSTR")]
    public char* ExportToRename;

    public D3D12_EXPORT_FLAGS Flags;
}
