namespace Ghost.DXGI;

public partial struct DXGI_SWAP_CHAIN_DESC
{
    public DXGI_MODE_DESC BufferDesc;

    public DXGI_SAMPLE_DESC SampleDesc;

    [NativeTypeName("DXGI_USAGE")]
    public uint BufferUsage;

    public uint BufferCount;

    public HWND OutputWindow;

    [NativeTypeName("BOOL")]
    public int Windowed;

    public DXGI_SWAP_EFFECT SwapEffect;

    public uint Flags;
}
