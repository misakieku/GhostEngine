namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_MLIR_COMPUTE_GRAPH_SUPPORT
{
    [NativeTypeName("BOOL")]
    public int MultisubgraphPartitionsSupported;

    [NativeTypeName("BOOL")]
    public int CpuBindingSupported;
}
