namespace Ghost.D3D12;

public partial struct D3D12_BUFFER_SRV_BYTE_OFFSET
{
    [NativeTypeName("UINT64")]
    public ulong Offset;

    [NativeTypeName("UINT64")]
    public ulong Size;

    public uint StructureByteStride;

    public D3D12_BUFFER_SRV_FLAGS Flags;
}
