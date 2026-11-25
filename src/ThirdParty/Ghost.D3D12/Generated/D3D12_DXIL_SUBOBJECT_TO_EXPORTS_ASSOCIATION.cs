namespace Ghost.D3D12;

public unsafe partial struct D3D12_DXIL_SUBOBJECT_TO_EXPORTS_ASSOCIATION
{
    [NativeTypeName("LPCWSTR")]
    public char* SubobjectToAssociate;

    public uint NumExports;

    [NativeTypeName("LPCWSTR *")]
    public char** pExports;
}
