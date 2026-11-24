namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_CROSS_NODE
{
    public D3D12_CROSS_NODE_SHARING_TIER SharingTier;

    [NativeTypeName("BOOL")]
    public int AtomicShaderInstructions;
}
