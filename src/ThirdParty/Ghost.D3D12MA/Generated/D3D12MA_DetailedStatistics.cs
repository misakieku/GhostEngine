namespace Ghost.D3D12MA;

public partial struct D3D12MA_DetailedStatistics
{
    public D3D12MA_Statistics Stats;

    public uint UnusedRangeCount;

    [NativeTypeName("UINT64")]
    public ulong AllocationSizeMin;

    [NativeTypeName("UINT64")]
    public ulong AllocationSizeMax;

    [NativeTypeName("UINT64")]
    public ulong UnusedRangeSizeMin;

    [NativeTypeName("UINT64")]
    public ulong UnusedRangeSizeMax;
}
