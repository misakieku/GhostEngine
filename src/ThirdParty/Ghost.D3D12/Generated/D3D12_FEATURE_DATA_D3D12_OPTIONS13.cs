namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS13
{
    [NativeTypeName("BOOL")]
    public int UnrestrictedBufferTextureCopyPitchSupported;

    [NativeTypeName("BOOL")]
    public int UnrestrictedVertexElementAlignmentSupported;

    [NativeTypeName("BOOL")]
    public int InvertedViewportHeightFlipsYSupported;

    [NativeTypeName("BOOL")]
    public int InvertedViewportDepthFlipsZSupported;

    [NativeTypeName("BOOL")]
    public int TextureCopyBetweenDimensionsSupported;

    [NativeTypeName("BOOL")]
    public int AlphaBlendFactorSupported;
}
