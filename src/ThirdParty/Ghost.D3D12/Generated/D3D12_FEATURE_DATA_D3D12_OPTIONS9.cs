namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS9
{
    [NativeTypeName("BOOL")]
    public int MeshShaderPipelineStatsSupported;

    [NativeTypeName("BOOL")]
    public int MeshShaderSupportsFullRangeRenderTargetArrayIndex;

    [NativeTypeName("BOOL")]
    public int AtomicInt64OnTypedResourceSupported;

    [NativeTypeName("BOOL")]
    public int AtomicInt64OnGroupSharedSupported;

    [NativeTypeName("BOOL")]
    public int DerivativesInMeshAndAmplificationShadersSupported;

    public D3D12_WAVE_MMA_TIER WaveMMATier;
}
