namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_VIRTUAL_ALLOCATION_DESC
{
    public D3D12MA_VIRTUAL_ALLOCATION_FLAGS Flags;

    [NativeTypeName("UINT64")]
    public ulong Size;

    [NativeTypeName("UINT64")]
    public ulong Alignment;

    public void* pPrivateData;
}
