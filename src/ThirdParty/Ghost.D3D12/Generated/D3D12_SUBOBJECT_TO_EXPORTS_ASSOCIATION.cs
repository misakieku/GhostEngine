namespace Ghost.D3D12;

public unsafe partial struct D3D12_SUBOBJECT_TO_EXPORTS_ASSOCIATION
{
    [NativeTypeName("const D3D12_STATE_SUBOBJECT *")]
    public D3D12_STATE_SUBOBJECT* pSubobjectToAssociate;

    public uint NumExports;

    [NativeTypeName("LPCWSTR *")]
    public char** pExports;
}
