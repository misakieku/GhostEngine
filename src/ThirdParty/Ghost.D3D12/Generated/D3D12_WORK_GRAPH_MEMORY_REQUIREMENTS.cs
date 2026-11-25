namespace Ghost.D3D12;

public partial struct D3D12_WORK_GRAPH_MEMORY_REQUIREMENTS
{
    [NativeTypeName("UINT64")]
    public ulong MinSizeInBytes;

    [NativeTypeName("UINT64")]
    public ulong MaxSizeInBytes;

    public uint SizeGranularityInBytes;
}
