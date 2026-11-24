namespace Ghost.D3D12;

public unsafe partial struct D3D12_STREAM_OUTPUT_DESC
{
    [NativeTypeName("const D3D12_SO_DECLARATION_ENTRY *")]
    public D3D12_SO_DECLARATION_ENTRY* pSODeclaration;

    public uint NumEntries;

    [NativeTypeName("const UINT *")]
    public uint* pBufferStrides;

    public uint NumStrides;

    public uint RasterizedStream;
}
