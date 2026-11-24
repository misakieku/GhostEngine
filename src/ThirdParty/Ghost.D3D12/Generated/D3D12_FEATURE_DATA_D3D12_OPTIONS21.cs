namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS21
{
    public D3D12_WORK_GRAPHS_TIER WorkGraphsTier;

    public D3D12_EXECUTE_INDIRECT_TIER ExecuteIndirectTier;

    [NativeTypeName("BOOL")]
    public int SampleCmpGradientAndBiasSupported;

    [NativeTypeName("BOOL")]
    public int ExtendedCommandInfoSupported;
}
