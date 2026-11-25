namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO
{
    [NativeTypeName("UINT64")]
    public ulong ResultDataMaxSizeInBytes;

    [NativeTypeName("UINT64")]
    public ulong ScratchDataSizeInBytes;

    [NativeTypeName("UINT64")]
    public ulong UpdateScratchDataSizeInBytes;
}
