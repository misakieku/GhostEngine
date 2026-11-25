namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS19
{
    [NativeTypeName("BOOL")]
    public int MismatchingOutputDimensionsSupported;

    public uint SupportedSampleCountsWithNoOutputs;

    [NativeTypeName("BOOL")]
    public int PointSamplingAddressesNeverRoundUp;

    [NativeTypeName("BOOL")]
    public int RasterizerDesc2Supported;

    [NativeTypeName("BOOL")]
    public int NarrowQuadrilateralLinesSupported;

    [NativeTypeName("BOOL")]
    public int AnisoFilterWithPointMipSupported;

    public uint MaxSamplerDescriptorHeapSize;

    public uint MaxSamplerDescriptorHeapSizeWithStaticSamplers;

    public uint MaxViewDescriptorHeapSize;

    [NativeTypeName("BOOL")]
    public int ComputeOnlyCustomHeapSupported;
}
