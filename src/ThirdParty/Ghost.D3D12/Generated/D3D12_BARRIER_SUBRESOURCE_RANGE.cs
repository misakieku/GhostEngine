namespace Ghost.D3D12;

public partial struct D3D12_BARRIER_SUBRESOURCE_RANGE
{
    public uint IndexOrFirstMipLevel;

    public uint NumMipLevels;

    public uint FirstArraySlice;

    public uint NumArraySlices;

    public uint FirstPlane;

    public uint NumPlanes;
}
