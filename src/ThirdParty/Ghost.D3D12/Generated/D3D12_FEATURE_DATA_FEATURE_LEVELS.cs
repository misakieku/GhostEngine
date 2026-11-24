namespace Ghost.D3D12;

public unsafe partial struct D3D12_FEATURE_DATA_FEATURE_LEVELS
{
    public uint NumFeatureLevels;

    [NativeTypeName("const D3D_FEATURE_LEVEL *")]
    public D3D_FEATURE_LEVEL* pFeatureLevelsRequested;

    public D3D_FEATURE_LEVEL MaxSupportedFeatureLevel;
}
