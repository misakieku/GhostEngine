namespace Ghost.D3D12;

public unsafe partial struct D3D12_TEXTURE_TEXTURE_COPY_DESC
{
    public ID3D12Resource* pDest;

    public uint DestSubresourceIndex;

    public ID3D12Resource* pSource;

    public uint SourceSubresourceIndex;
}
