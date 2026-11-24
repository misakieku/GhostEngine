namespace Ghost.D3D12;

public partial struct D3D12_RESOURCE_ALLOCATION_INFO
{
    [NativeTypeName("UINT64")]
    public ulong SizeInBytes;

    [NativeTypeName("UINT64")]
    public ulong Alignment;
}
