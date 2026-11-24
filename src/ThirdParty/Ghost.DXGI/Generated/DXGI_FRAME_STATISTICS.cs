namespace Ghost.DXGI;

public partial struct DXGI_FRAME_STATISTICS
{
    public uint PresentCount;

    public uint PresentRefreshCount;

    public uint SyncRefreshCount;

    public LARGE_INTEGER SyncQPCTime;

    public LARGE_INTEGER SyncGPUTime;
}
