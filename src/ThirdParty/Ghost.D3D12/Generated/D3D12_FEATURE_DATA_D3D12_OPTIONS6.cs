namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS6
{
    [NativeTypeName("BOOL")]
    public int AdditionalShadingRatesSupported;

    [NativeTypeName("BOOL")]
    public int PerPrimitiveShadingRateSupportedWithViewportIndexing;

    public D3D12_VARIABLE_SHADING_RATE_TIER VariableShadingRateTier;

    public uint ShadingRateImageTileSize;

    [NativeTypeName("BOOL")]
    public int BackgroundProcessingSupported;
}
