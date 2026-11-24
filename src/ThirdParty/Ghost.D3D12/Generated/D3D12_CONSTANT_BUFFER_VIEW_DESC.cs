namespace Ghost.D3D12;

public partial struct D3D12_CONSTANT_BUFFER_VIEW_DESC
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong BufferLocation;

    public uint SizeInBytes;
}
