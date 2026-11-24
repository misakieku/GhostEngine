namespace Ghost.D3D12;

public partial struct D3D12_COMMAND_QUEUE_DESC
{
    public D3D12_COMMAND_LIST_TYPE Type;

    public int Priority;

    public D3D12_COMMAND_QUEUE_FLAGS Flags;

    public uint NodeMask;
}
