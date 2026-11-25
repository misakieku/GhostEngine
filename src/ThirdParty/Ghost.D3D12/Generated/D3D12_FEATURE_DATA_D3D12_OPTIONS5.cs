namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS5
{
    [NativeTypeName("BOOL")]
    public int SRVOnlyTiledResourceTier3;

    public D3D12_RENDER_PASS_TIER RenderPassesTier;

    public D3D12_RAYTRACING_TIER RaytracingTier;
}
