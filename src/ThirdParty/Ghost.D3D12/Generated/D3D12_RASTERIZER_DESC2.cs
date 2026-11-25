namespace Ghost.D3D12;

public partial struct D3D12_RASTERIZER_DESC2
{
    public D3D12_FILL_MODE FillMode;

    public D3D12_CULL_MODE CullMode;

    [NativeTypeName("BOOL")]
    public int FrontCounterClockwise;

    public float DepthBias;

    public float DepthBiasClamp;

    public float SlopeScaledDepthBias;

    [NativeTypeName("BOOL")]
    public int DepthClipEnable;

    public D3D12_LINE_RASTERIZATION_MODE LineRasterizationMode;

    public uint ForcedSampleCount;

    public D3D12_CONSERVATIVE_RASTERIZATION_MODE ConservativeRaster;
}
