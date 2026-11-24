namespace Ghost.D3D12MA;

public partial struct D3D12MA_DEFRAGMENTATION_DESC
{
    public D3D12MA_DEFRAGMENTATION_FLAGS Flags;

    [NativeTypeName("UINT64")]
    public ulong MaxBytesPerPass;

    [NativeTypeName("UINT32")]
    public uint MaxAllocationsPerPass;
}
