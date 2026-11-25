namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_GEOMETRY_AABBS_DESC
{
    [NativeTypeName("UINT64")]
    public ulong AABBCount;

    public D3D12_GPU_VIRTUAL_ADDRESS_AND_STRIDE AABBs;
}
