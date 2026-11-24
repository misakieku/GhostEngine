namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_GEOMETRY_OMM_LINKAGE_DESC
{
    public D3D12_GPU_VIRTUAL_ADDRESS_AND_STRIDE OpacityMicromapIndexBuffer;

    public DXGI_FORMAT OpacityMicromapIndexFormat;

    public uint OpacityMicromapBaseLocation;

    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong OpacityMicromapArray;
}
