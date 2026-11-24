namespace Ghost.D3D12;

public partial struct D3D12_HEAP_DESC
{
    [NativeTypeName("UINT64")]
    public ulong SizeInBytes;

    public D3D12_HEAP_PROPERTIES Properties;

    [NativeTypeName("UINT64")]
    public ulong Alignment;

    public D3D12_HEAP_FLAGS Flags;
}
