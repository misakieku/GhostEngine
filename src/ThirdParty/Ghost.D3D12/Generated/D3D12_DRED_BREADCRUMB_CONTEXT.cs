namespace Ghost.D3D12;

public unsafe partial struct D3D12_DRED_BREADCRUMB_CONTEXT
{
    public uint BreadcrumbIndex;

    [NativeTypeName("const wchar_t *")]
    public char* pContextString;
}
