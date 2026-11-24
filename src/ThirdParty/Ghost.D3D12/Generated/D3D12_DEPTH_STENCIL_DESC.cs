namespace Ghost.D3D12;

public partial struct D3D12_DEPTH_STENCIL_DESC
{
    [NativeTypeName("BOOL")]
    public int DepthEnable;

    public D3D12_DEPTH_WRITE_MASK DepthWriteMask;

    public D3D12_COMPARISON_FUNC DepthFunc;

    [NativeTypeName("BOOL")]
    public int StencilEnable;

    [NativeTypeName("UINT8")]
    public byte StencilReadMask;

    [NativeTypeName("UINT8")]
    public byte StencilWriteMask;

    public D3D12_DEPTH_STENCILOP_DESC FrontFace;

    public D3D12_DEPTH_STENCILOP_DESC BackFace;
}
