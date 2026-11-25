namespace Ghost.D3D12;

public partial struct D3D12_BUFFER_RTV
{
    [NativeTypeName("UINT64")]
    public ulong FirstElement;

    public uint NumElements;
}
