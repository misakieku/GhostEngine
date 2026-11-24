namespace Ghost.D3D12;

public partial struct D3D12_VERTEX_BUFFER_VIEW
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong BufferLocation;

    public uint SizeInBytes;

    public uint StrideInBytes;
}
