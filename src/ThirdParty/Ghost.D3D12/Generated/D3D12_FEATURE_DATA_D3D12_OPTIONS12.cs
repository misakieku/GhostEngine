namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS12
{
    public D3D12_TRI_STATE MSPrimitivesPipelineStatisticIncludesCulledPrimitives;

    [NativeTypeName("BOOL")]
    public int EnhancedBarriersSupported;

    [NativeTypeName("BOOL")]
    public int RelaxedFormatCastingSupported;
}
