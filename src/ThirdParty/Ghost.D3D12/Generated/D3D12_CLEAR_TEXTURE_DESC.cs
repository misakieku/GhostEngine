namespace Ghost.D3D12;

public unsafe partial struct D3D12_CLEAR_TEXTURE_DESC
{
    public ID3D12Resource* pTexture;

    public uint SubresourceIndex;

    public D3D12_CLEAR_DATA ClearValue;

    public DXGI_FORMAT Format;

    [NativeTypeName("const D3D12_BOX *")]
    public D3D12_BOX* pRegion;
}
