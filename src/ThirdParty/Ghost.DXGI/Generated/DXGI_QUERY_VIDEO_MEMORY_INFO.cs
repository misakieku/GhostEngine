namespace Ghost.DXGI;

public partial struct DXGI_QUERY_VIDEO_MEMORY_INFO
{
    [NativeTypeName("UINT64")]
    public ulong Budget;

    [NativeTypeName("UINT64")]
    public ulong CurrentUsage;

    [NativeTypeName("UINT64")]
    public ulong AvailableForReservation;

    [NativeTypeName("UINT64")]
    public ulong CurrentReservation;
}
