namespace Ghost.D3D12;

public partial struct D3D12_BUFFER_UAV
{
    [NativeTypeName("UINT64")]
    public ulong FirstElement;

    public uint NumElements;

    public uint StructureByteStride;

    [NativeTypeName("UINT64")]
    public ulong CounterOffsetInBytes;

    public D3D12_BUFFER_UAV_FLAGS Flags;
}
