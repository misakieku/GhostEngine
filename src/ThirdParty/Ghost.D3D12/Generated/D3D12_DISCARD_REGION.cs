namespace Ghost.D3D12;

public unsafe partial struct D3D12_DISCARD_REGION
{
    public uint NumRects;

    [NativeTypeName("const D3D12_RECT *")]
    public RECT* pRects;

    public uint FirstSubresource;

    public uint NumSubresources;
}
