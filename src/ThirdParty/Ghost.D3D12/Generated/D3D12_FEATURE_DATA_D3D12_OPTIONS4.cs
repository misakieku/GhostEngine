namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS4
{
    [NativeTypeName("BOOL")]
    public int MSAA64KBAlignedTextureSupported;

    public D3D12_SHARED_RESOURCE_COMPATIBILITY_TIER SharedResourceCompatibilityTier;

    [NativeTypeName("BOOL")]
    public int Native16BitShaderOpsSupported;
}
