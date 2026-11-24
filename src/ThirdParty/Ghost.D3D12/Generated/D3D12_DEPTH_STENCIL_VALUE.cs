namespace Ghost.D3D12;

public partial struct D3D12_DEPTH_STENCIL_VALUE
{
    public float Depth;

    [NativeTypeName("UINT8")]
    public byte Stencil;
}
