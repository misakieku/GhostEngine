namespace Ghost.D3D12;

public partial struct D3D12_RESOURCE_ALLOCATION_INFO1
{
    [NativeTypeName("UINT64")]
    public ulong Offset;

    [NativeTypeName("UINT64")]
    public ulong Alignment;

    [NativeTypeName("UINT64")]
    public ulong SizeInBytes;
}
