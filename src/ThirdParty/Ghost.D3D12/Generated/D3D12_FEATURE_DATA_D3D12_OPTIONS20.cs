namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS20
{
    [NativeTypeName("BOOL")]
    public int ComputeOnlyWriteWatchSupported;

    public D3D12_RECREATE_AT_TIER RecreateAtTier;
}
