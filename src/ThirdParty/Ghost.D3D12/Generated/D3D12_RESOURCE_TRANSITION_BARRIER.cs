namespace Ghost.D3D12;

public unsafe partial struct D3D12_RESOURCE_TRANSITION_BARRIER
{
    public ID3D12Resource* pResource;

    public uint Subresource;

    public D3D12_RESOURCE_STATES StateBefore;

    public D3D12_RESOURCE_STATES StateAfter;
}
