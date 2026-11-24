namespace Ghost.D3D12;

public partial struct D3D12_DEPTH_STENCIL_DESC2
{
    [NativeTypeName("BOOL")]
    public int DepthEnable;

    public D3D12_DEPTH_WRITE_MASK DepthWriteMask;

    public D3D12_COMPARISON_FUNC DepthFunc;

    [NativeTypeName("BOOL")]
    public int StencilEnable;

    public D3D12_DEPTH_STENCILOP_DESC1 FrontFace;

    public D3D12_DEPTH_STENCILOP_DESC1 BackFace;

    [NativeTypeName("BOOL")]
    public int DepthBoundsTestEnable;
}
