namespace Ghost.D3D12;

public unsafe partial struct D3D12_DRED_ALLOCATION_NODE
{
    [NativeTypeName("const char *")]
    public sbyte* ObjectNameA;

    [NativeTypeName("const wchar_t *")]
    public char* ObjectNameW;

    public D3D12_DRED_ALLOCATION_TYPE AllocationType;

    [NativeTypeName("const struct D3D12_DRED_ALLOCATION_NODE *")]
    public D3D12_DRED_ALLOCATION_NODE* pNext;
}
