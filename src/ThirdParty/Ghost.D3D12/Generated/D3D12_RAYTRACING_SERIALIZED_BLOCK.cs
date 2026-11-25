namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_SERIALIZED_BLOCK
{
    [NativeTypeName("D3D12_RAYTRACING_SERIALIZED_BLOCK_TYPE")]
    public D3D12_SERIALIZED_BLOCK_TYPE Type;

    [NativeTypeName("UINT64")]
    public ulong NumBlockPointersAfterHeader;
}
