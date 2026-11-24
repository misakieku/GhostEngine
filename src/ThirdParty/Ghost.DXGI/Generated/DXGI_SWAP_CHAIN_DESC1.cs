namespace Ghost.DXGI;

public partial struct DXGI_SWAP_CHAIN_DESC1
{
    public uint Width;

    public uint Height;

    public DXGI_FORMAT Format;

    [NativeTypeName("BOOL")]
    public int Stereo;

    public DXGI_SAMPLE_DESC SampleDesc;

    [NativeTypeName("DXGI_USAGE")]
    public uint BufferUsage;

    public uint BufferCount;

    public DXGI_SCALING Scaling;

    public DXGI_SWAP_EFFECT SwapEffect;

    public DXGI_ALPHA_MODE AlphaMode;

    public uint Flags;
}
