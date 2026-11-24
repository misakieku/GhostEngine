namespace Ghost.D3D12MA;

public partial struct D3D12MA_DEFRAGMENTATION_STATS
{
    [NativeTypeName("UINT64")]
    public ulong BytesMoved;

    [NativeTypeName("UINT64")]
    public ulong BytesFreed;

    [NativeTypeName("UINT32")]
    public uint AllocationsMoved;

    [NativeTypeName("UINT32")]
    public uint HeapsFreed;
}
