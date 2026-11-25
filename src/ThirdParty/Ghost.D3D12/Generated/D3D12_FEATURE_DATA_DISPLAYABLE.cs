namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_DISPLAYABLE
{
    [NativeTypeName("BOOL")]
    public int DisplayableTexture;

    public D3D12_SHARED_RESOURCE_COMPATIBILITY_TIER SharedResourceCompatibilityTier;
}
