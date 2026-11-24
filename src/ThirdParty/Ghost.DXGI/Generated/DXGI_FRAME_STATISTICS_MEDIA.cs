namespace Ghost.DXGI;

public partial struct DXGI_FRAME_STATISTICS_MEDIA
{
    public uint PresentCount;

    public uint PresentRefreshCount;

    public uint SyncRefreshCount;

    public LARGE_INTEGER SyncQPCTime;

    public LARGE_INTEGER SyncGPUTime;

    public DXGI_FRAME_PRESENTATION_MODE CompositionMode;

    public uint ApprovedPresentDuration;
}
