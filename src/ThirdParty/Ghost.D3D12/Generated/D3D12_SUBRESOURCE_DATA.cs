namespace Ghost.D3D12;

public unsafe partial struct D3D12_SUBRESOURCE_DATA
{
    [NativeTypeName("const void *")]
    public void* pData;

    [NativeTypeName("LONG_PTR")]
    public nint RowPitch;

    [NativeTypeName("LONG_PTR")]
    public nint SlicePitch;
}
