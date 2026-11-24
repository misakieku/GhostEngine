namespace Ghost.D3D12;

public unsafe partial struct D3D12_DEVICE_REMOVED_EXTENDED_DATA
{
    public D3D12_DRED_FLAGS Flags;

    public D3D12_AUTO_BREADCRUMB_NODE* pHeadAutoBreadcrumbNode;
}
