namespace Ghost.D3D12;

public unsafe partial struct D3D12_STATE_OBJECT_DESC
{
    public D3D12_STATE_OBJECT_TYPE Type;

    public uint NumSubobjects;

    [NativeTypeName("const D3D12_STATE_SUBOBJECT *")]
    public D3D12_STATE_SUBOBJECT* pSubobjects;
}
