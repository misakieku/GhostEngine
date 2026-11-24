namespace Ghost.D3D12;

public unsafe partial struct D3D12_CACHE_CONST_VALUE
{
    [NativeTypeName("const void *")]
    public void* pValue;

    [NativeTypeName("SIZE_T")]
    public nuint ValueSize;
}
