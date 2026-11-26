namespace Ghost.D3D12;

public partial struct D3D12_RESOURCE_DESC2
{
    public D3D12_RESOURCE_DIMENSION Dimension;

    [NativeTypeName("UINT64")]
    public ulong Alignment;

    [NativeTypeName("UINT64")]
    public ulong Width;

    public uint Height;

    [NativeTypeName("UINT16")]
    public ushort DepthOrArraySize;

    [NativeTypeName("UINT16")]
    public ushort MipLevels;

    public DXGI_FORMAT Format;

    public DXGI_SAMPLE_DESC SampleDesc;

    public D3D12_TEXTURE_LAYOUT Layout;

    public D3D12_RESOURCE_FLAGS Flags;

    public D3D12_MIP_REGION SamplerFeedbackMipRegion;

    public Guid LayoutGuid;
}
