namespace Ghost.D3D12;

public partial struct D3D12_PLACED_SUBRESOURCE_FOOTPRINT
{
    [NativeTypeName("UINT64")]
    public ulong Offset;

    public D3D12_SUBRESOURCE_FOOTPRINT Footprint;
}
