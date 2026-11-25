namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_VIRTUAL_ALLOCATION_INFO
{
    [NativeTypeName("UINT64")]
    public ulong Offset;

    [NativeTypeName("UINT64")]
    public ulong Size;

    public void* pPrivateData;
}
