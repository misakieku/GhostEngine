namespace Ghost.D3D12;

public unsafe partial struct D3D12_NODE_CPU_INPUT
{
    public uint EntrypointIndex;

    public uint NumRecords;

    [NativeTypeName("const void *")]
    public void* pRecords;

    [NativeTypeName("UINT64")]
    public ulong RecordStrideInBytes;
}
