namespace Ghost.D3D12;

public unsafe partial struct D3D12_EXISTING_COLLECTION_BY_KEY_DESC
{
    [NativeTypeName("const void *")]
    public void* pKey;

    public uint KeySize;

    public uint NumExports;

    [NativeTypeName("const D3D12_EXPORT_DESC *")]
    public D3D12_EXPORT_DESC* pExports;
}
