namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_POOL_DESC
{
    public D3D12MA_POOL_FLAGS Flags;

    public D3D12_HEAP_PROPERTIES HeapProperties;

    public D3D12_HEAP_FLAGS HeapFlags;

    [NativeTypeName("UINT64")]
    public ulong BlockSize;

    public uint MinBlockCount;

    public uint MaxBlockCount;

    [NativeTypeName("UINT64")]
    public ulong MinAllocationAlignment;

    public ID3D12ProtectedResourceSession* pProtectedSession;

    public D3D12_RESIDENCY_PRIORITY ResidencyPriority;
}
