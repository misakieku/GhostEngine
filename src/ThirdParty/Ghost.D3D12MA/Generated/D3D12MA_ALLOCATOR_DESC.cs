namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_ALLOCATOR_DESC
{
    public D3D12MA_ALLOCATOR_FLAGS Flags;

    public ID3D12Device* pDevice;

    [NativeTypeName("UINT64")]
    public ulong PreferredBlockSize;

    [NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS *")]
    public D3D12MA_ALLOCATION_CALLBACKS* pAllocationCallbacks;

    public IDXGIAdapter* pAdapter;
}
