namespace Ghost.D3D12;

public unsafe partial struct D3D12_EXISTING_COLLECTION_DESC
{
    public ID3D12StateObject* pExistingCollection;

    public uint NumExports;

    [NativeTypeName("const D3D12_EXPORT_DESC *")]
    public D3D12_EXPORT_DESC* pExports;
}
