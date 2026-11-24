namespace Ghost.D3D12;

public unsafe partial struct D3D12_BUFFER_BARRIER
{
    public D3D12_BARRIER_SYNC SyncBefore;

    public D3D12_BARRIER_SYNC SyncAfter;

    public D3D12_BARRIER_ACCESS AccessBefore;

    public D3D12_BARRIER_ACCESS AccessAfter;

    public ID3D12Resource* pResource;

    [NativeTypeName("UINT64")]
    public ulong Offset;

    [NativeTypeName("UINT64")]
    public ulong Size;
}
