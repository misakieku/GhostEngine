namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS14
{
    [NativeTypeName("BOOL")]
    public int AdvancedTextureOpsSupported;

    [NativeTypeName("BOOL")]
    public int WriteableMSAATexturesSupported;

    [NativeTypeName("BOOL")]
    public int IndependentFrontAndBackStencilRefMaskSupported;
}
