namespace Ghost.D3D12;

public unsafe partial struct D3D12_RESOURCE_ALIASING_BARRIER
{
    public ID3D12Resource* pResourceBefore;

    public ID3D12Resource* pResourceAfter;
}
