namespace Ghost.D3D12;

public partial struct D3D12_GPU_VIRTUAL_ADDRESS_AND_STRIDE
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong StartAddress;

    [NativeTypeName("UINT64")]
    public ulong StrideInBytes;
}
