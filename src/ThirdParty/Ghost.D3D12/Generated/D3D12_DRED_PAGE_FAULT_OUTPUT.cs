namespace Ghost.D3D12;

public unsafe partial struct D3D12_DRED_PAGE_FAULT_OUTPUT
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong PageFaultVA;

    [NativeTypeName("const D3D12_DRED_ALLOCATION_NODE *")]
    public D3D12_DRED_ALLOCATION_NODE* pHeadExistingAllocationNode;

    [NativeTypeName("const D3D12_DRED_ALLOCATION_NODE *")]
    public D3D12_DRED_ALLOCATION_NODE* pHeadRecentFreedAllocationNode;
}
