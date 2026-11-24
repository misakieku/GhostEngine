namespace Ghost.D3D12;

public partial struct D3D12_STREAM_OUTPUT_BUFFER_VIEW
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong BufferLocation;

    [NativeTypeName("UINT64")]
    public ulong SizeInBytes;

    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong BufferFilledSizeLocation;
}
