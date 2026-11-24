namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_ARCHITECTURE
{
    public uint NodeIndex;

    [NativeTypeName("BOOL")]
    public int TileBasedRenderer;

    [NativeTypeName("BOOL")]
    public int UMA;

    [NativeTypeName("BOOL")]
    public int CacheCoherentUMA;
}
