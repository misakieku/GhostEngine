namespace Ghost.D3D12;

public partial struct D3D12_BUFFER_UAV_BYTE_OFFSET
{
    [NativeTypeName("UINT64")]
    public ulong Offset;

    [NativeTypeName("UINT64")]
    public ulong Size;

    public uint StructureByteStride;

    [NativeTypeName("UINT64")]
    public ulong CounterOffsetInBytes;

    public D3D12_BUFFER_UAV_FLAGS Flags;
}
