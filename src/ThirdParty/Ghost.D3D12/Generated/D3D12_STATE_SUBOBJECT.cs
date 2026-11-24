namespace Ghost.D3D12;

public unsafe partial struct D3D12_STATE_SUBOBJECT
{
    public D3D12_STATE_SUBOBJECT_TYPE Type;

    [NativeTypeName("const void *")]
    public void* pDesc;
}
