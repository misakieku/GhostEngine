namespace Ghost.D3D12;

public partial struct D3D12_WRITEBUFFERIMMEDIATE_PARAMETER
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong Dest;

    [NativeTypeName("UINT32")]
    public uint Value;
}
