namespace Ghost.D3D12;

public partial struct D3D12_STATE_OBJECT_STATISTICS
{
    [NativeTypeName("BOOL")]
    public int DefaultPSDBRegistered;

    public D3D12_CREATE_STATE_OBJECT_STATISTICS PipelineStateObjectStatistics;

    public D3D12_CREATE_STATE_OBJECT_STATISTICS StateObjectStatistics;
}
