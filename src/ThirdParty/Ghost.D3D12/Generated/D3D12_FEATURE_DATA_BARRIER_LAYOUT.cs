namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_BARRIER_LAYOUT
{
    public D3D12_COMMAND_LIST_TYPE CommandListType;

    public D3D12_BARRIER_LAYOUT Layout;

    [NativeTypeName("BOOL")]
    public int Supported;
}
