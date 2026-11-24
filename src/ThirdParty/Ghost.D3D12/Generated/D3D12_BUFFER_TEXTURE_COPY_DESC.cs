namespace Ghost.D3D12;

public unsafe partial struct D3D12_BUFFER_TEXTURE_COPY_DESC
{
    public ID3D12Resource* pBuffer;

    public ID3D12Resource* pTexture;

    public uint SubresourceIndex;

    public D3D12_PLACED_SUBRESOURCE_FOOTPRINT PlacedFootprint;
}
