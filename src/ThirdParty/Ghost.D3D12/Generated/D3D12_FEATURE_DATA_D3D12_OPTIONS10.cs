namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS10
{
    [NativeTypeName("BOOL")]
    public int VariableRateShadingSumCombinerSupported;

    [NativeTypeName("BOOL")]
    public int MeshShaderPerPrimitiveShadingRateSupported;
}
