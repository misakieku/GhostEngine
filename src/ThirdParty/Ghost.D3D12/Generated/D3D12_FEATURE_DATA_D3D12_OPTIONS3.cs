namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS3
{
    [NativeTypeName("BOOL")]
    public int CopyQueueTimestampQueriesSupported;

    [NativeTypeName("BOOL")]
    public int CastingFullyTypedFormatSupported;

    public D3D12_COMMAND_LIST_SUPPORT_FLAGS WriteBufferImmediateSupportFlags;

    public D3D12_VIEW_INSTANCING_TIER ViewInstancingTier;

    [NativeTypeName("BOOL")]
    public int BarycentricsSupported;
}
