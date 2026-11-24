namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_GEOMETRY_TRIANGLES_DESC
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong Transform3x4;

    public DXGI_FORMAT IndexFormat;

    public DXGI_FORMAT VertexFormat;

    public uint IndexCount;

    public uint VertexCount;

    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong IndexBuffer;

    public D3D12_GPU_VIRTUAL_ADDRESS_AND_STRIDE VertexBuffer;
}
