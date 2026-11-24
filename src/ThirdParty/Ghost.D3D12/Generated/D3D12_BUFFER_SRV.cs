namespace Ghost.D3D12;

public partial struct D3D12_BUFFER_SRV
{
    [NativeTypeName("UINT64")]
    public ulong FirstElement;

    public uint NumElements;

    public uint StructureByteStride;

    public D3D12_BUFFER_SRV_FLAGS Flags;
}
