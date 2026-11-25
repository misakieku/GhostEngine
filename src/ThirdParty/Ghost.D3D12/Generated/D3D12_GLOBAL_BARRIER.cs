namespace Ghost.D3D12;

public partial struct D3D12_GLOBAL_BARRIER
{
    public D3D12_BARRIER_SYNC SyncBefore;

    public D3D12_BARRIER_SYNC SyncAfter;

    public D3D12_BARRIER_ACCESS AccessBefore;

    public D3D12_BARRIER_ACCESS AccessAfter;
}
