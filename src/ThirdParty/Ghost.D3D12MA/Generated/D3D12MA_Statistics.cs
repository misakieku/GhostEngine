namespace Ghost.D3D12MA;

public partial struct D3D12MA_Statistics
{
    public uint BlockCount;

    public uint AllocationCount;

    [NativeTypeName("UINT64")]
    public ulong BlockBytes;

    [NativeTypeName("UINT64")]
    public ulong AllocationBytes;
}
