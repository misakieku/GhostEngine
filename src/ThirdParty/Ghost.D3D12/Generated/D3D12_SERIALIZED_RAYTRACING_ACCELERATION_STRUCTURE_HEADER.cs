namespace Ghost.D3D12;

public partial struct D3D12_SERIALIZED_RAYTRACING_ACCELERATION_STRUCTURE_HEADER
{
    public D3D12_SERIALIZED_DATA_DRIVER_MATCHING_IDENTIFIER DriverMatchingIdentifier;

    [NativeTypeName("UINT64")]
    public ulong SerializedSizeInBytesIncludingHeader;

    [NativeTypeName("UINT64")]
    public ulong DeserializedSizeInBytes;

    [NativeTypeName("UINT64")]
    public ulong NumBottomLevelAccelerationStructurePointersAfterHeader;
}
