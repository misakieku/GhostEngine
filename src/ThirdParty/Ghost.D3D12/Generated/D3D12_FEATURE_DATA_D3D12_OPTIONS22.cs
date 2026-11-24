namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS22
{
    [NativeTypeName("BOOL")]
    public int ShaderExecutionReorderingActuallyReorders;

    [NativeTypeName("BOOL")]
    public int CreateByteOffsetViewsSupported;

    public uint Max1DDispatchSize;

    public uint Max1DDispatchMeshSize;
}
