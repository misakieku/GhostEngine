namespace Ghost.D3D12;

public partial struct D3D12_RENDER_PASS_ENDING_ACCESS_RESOLVE_SUBRESOURCE_PARAMETERS
{
    public uint SrcSubresource;

    public uint DstSubresource;

    public uint DstX;

    public uint DstY;

    [NativeTypeName("D3D12_RECT")]
    public RECT SrcRect;
}
