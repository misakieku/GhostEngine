namespace Ghost.D3D12;

public partial struct D3D12_SHADER_CACHE_SESSION_INFO
{
    [NativeTypeName("UINT64")]
    public ulong CurrentSizeBytes;

    [NativeTypeName("UINT64")]
    public ulong MaximumSizeBytes;

    [NativeTypeName("UINT64")]
    public ulong ReservedSizeBytes;

    public uint EntryCount;

    [NativeTypeName("UINT64")]
    public ulong LifetimeEvictedEntryCount;
}
