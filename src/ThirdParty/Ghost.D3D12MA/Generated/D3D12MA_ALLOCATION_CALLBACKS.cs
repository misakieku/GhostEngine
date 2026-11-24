namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_ALLOCATION_CALLBACKS
{
    [NativeTypeName("D3D12MA_ALLOCATE_FUNC_PTR")]
    public delegate* unmanaged[Cdecl]<nuint, nuint, void*, void*> pAllocate;

    [NativeTypeName("D3D12MA_FREE_FUNC_PTR")]
    public delegate* unmanaged[Cdecl]<void*, void*, void> pFree;

    public void* pPrivateData;
}
