namespace Ghost.D3D12;

public partial struct D3D12_DEVICE_REMOVED_EXTENDED_DATA1
{
    [NativeTypeName("HRESULT")]
    public int DeviceRemovedReason;

    public D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT AutoBreadcrumbsOutput;

    public D3D12_DRED_PAGE_FAULT_OUTPUT PageFaultOutput;
}
