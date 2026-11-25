namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_VIRTUAL_BLOCK_DESC
{
    public D3D12MA_VIRTUAL_BLOCK_FLAGS Flags;

    [NativeTypeName("UINT64")]
    public ulong Size;

    [NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS *")]
    public D3D12MA_ALLOCATION_CALLBACKS* pAllocationCallbacks;
}
