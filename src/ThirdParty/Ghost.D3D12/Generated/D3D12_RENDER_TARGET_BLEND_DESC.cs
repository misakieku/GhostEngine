namespace Ghost.D3D12;

public partial struct D3D12_RENDER_TARGET_BLEND_DESC
{
    [NativeTypeName("BOOL")]
    public int BlendEnable;

    [NativeTypeName("BOOL")]
    public int LogicOpEnable;

    public D3D12_BLEND SrcBlend;

    public D3D12_BLEND DestBlend;

    public D3D12_BLEND_OP BlendOp;

    public D3D12_BLEND SrcBlendAlpha;

    public D3D12_BLEND DestBlendAlpha;

    public D3D12_BLEND_OP BlendOpAlpha;

    public D3D12_LOGIC_OP LogicOp;

    [NativeTypeName("UINT8")]
    public byte RenderTargetWriteMask;
}
