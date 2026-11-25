namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_ALLOCATION_DESC
{
    public D3D12MA_ALLOCATION_FLAGS Flags;

    public D3D12_HEAP_TYPE HeapType;

    public D3D12_HEAP_FLAGS ExtraHeapFlags;

    public D3D12MA_Pool* CustomPool;

    public void* pPrivateData;
}
