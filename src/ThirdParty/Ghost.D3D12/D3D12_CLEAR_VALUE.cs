namespace Ghost.D3D12;

public unsafe partial struct D3D12_CLEAR_VALUE
{
    public D3D12_CLEAR_VALUE(DXGI_FORMAT format, float* color)
    {
        Format = format;
        Anonymous.Color[0] = color[0];
        Anonymous.Color[1] = color[1];
        Anonymous.Color[2] = color[2];
        Anonymous.Color[3] = color[3];
    }

    public D3D12_CLEAR_VALUE(DXGI_FORMAT format, float depth, byte stencil)
    {
        Format = format;
        Anonymous.DepthStencil = new D3D12_DEPTH_STENCIL_VALUE
        {
            Depth = depth,
            Stencil = stencil
        };
    }
}
