namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_COMMAND_QUEUE_PRIORITY
{
    public D3D12_COMMAND_LIST_TYPE CommandListType;

    public uint Priority;

    [NativeTypeName("BOOL")]
    public int PriorityForTypeIsSupported;
}
