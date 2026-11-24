namespace Ghost.D3D12;

public partial struct D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong StartAddress;

    [NativeTypeName("UINT64")]
    public ulong SizeInBytes;

    [NativeTypeName("UINT64")]
    public ulong StrideInBytes;
}
