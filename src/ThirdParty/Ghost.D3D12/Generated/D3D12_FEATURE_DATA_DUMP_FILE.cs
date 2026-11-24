namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_DUMP_FILE
{
    [NativeTypeName("BOOL")]
    public int SupportedByOS;

    public D3D12_DUMP_FILE_DRIVER_TIER DumpFileDriverTier;

    public uint DumpFileDriverOptionsMask;
}
