namespace Ghost.D3D12;

public unsafe partial struct D3D12_AUTO_BREADCRUMB_NODE
{
    [NativeTypeName("const char *")]
    public sbyte* pCommandListDebugNameA;

    [NativeTypeName("const wchar_t *")]
    public char* pCommandListDebugNameW;

    [NativeTypeName("const char *")]
    public sbyte* pCommandQueueDebugNameA;

    [NativeTypeName("const wchar_t *")]
    public char* pCommandQueueDebugNameW;

    public ID3D12GraphicsCommandList* pCommandList;

    public ID3D12CommandQueue* pCommandQueue;

    [NativeTypeName("UINT32")]
    public uint BreadcrumbCount;

    [NativeTypeName("const UINT32 *")]
    public uint* pLastBreadcrumbValue;

    [NativeTypeName("const D3D12_AUTO_BREADCRUMB_OP *")]
    public D3D12_AUTO_BREADCRUMB_OP* pCommandHistory;

    [NativeTypeName("const struct D3D12_AUTO_BREADCRUMB_NODE *")]
    public D3D12_AUTO_BREADCRUMB_NODE* pNext;
}
