namespace Ghost.D3D12;

public partial struct D3D12_QUERY_DATA_SO_STATISTICS
{
    [NativeTypeName("UINT64")]
    public ulong NumPrimitivesWritten;

    [NativeTypeName("UINT64")]
    public ulong PrimitivesStorageNeeded;
}
