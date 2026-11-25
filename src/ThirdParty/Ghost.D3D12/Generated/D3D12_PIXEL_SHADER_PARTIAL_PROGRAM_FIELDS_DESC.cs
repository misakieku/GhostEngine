namespace Ghost.D3D12;

public partial struct D3D12_PIXEL_SHADER_PARTIAL_PROGRAM_FIELDS_DESC
{
    public D3D12_LINE_RASTERIZATION_MODE LineRasterizationMode;

    public uint ForcedSampleCount;

    [NativeTypeName("BOOL")]
    public int AlphaToCoverageEnable;

    [NativeTypeName("BOOL")]
    public int DualSourceBlendEnable;

    [NativeTypeName("BOOL")]
    public int LateLinkRasterizerSubobject;

    [NativeTypeName("BOOL")]
    public int LateLinkBlendSubobject;

    [NativeTypeName("BOOL")]
    public int LateLinkSampleMaskSubobject;

    [NativeTypeName("BOOL")]
    public int LateLinkSampleDescSubobject;

    [NativeTypeName("BOOL")]
    public int LateLinkDepthStencilFormatSubobject;

    [NativeTypeName("BOOL")]
    public int LateLinkRenderTargetFormatSubobject;

    [NativeTypeName("BOOL")]
    public int LateLinkDepthStencilSubobject;
}
