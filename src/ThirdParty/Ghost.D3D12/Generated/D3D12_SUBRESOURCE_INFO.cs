namespace Ghost.D3D12;

public partial struct D3D12_SUBRESOURCE_INFO
{
    [NativeTypeName("UINT64")]
    public ulong Offset;

    public uint RowPitch;

    public uint DepthPitch;
}
