namespace Ghost.D3D12;

public unsafe partial struct D3D12_TEXTURE_BARRIER
{
    public D3D12_BARRIER_SYNC SyncBefore;

    public D3D12_BARRIER_SYNC SyncAfter;

    public D3D12_BARRIER_ACCESS AccessBefore;

    public D3D12_BARRIER_ACCESS AccessAfter;

    public D3D12_BARRIER_LAYOUT LayoutBefore;

    public D3D12_BARRIER_LAYOUT LayoutAfter;

    public ID3D12Resource* pResource;

    public D3D12_BARRIER_SUBRESOURCE_RANGE Subresources;

    public D3D12_TEXTURE_BARRIER_FLAGS Flags;
}
