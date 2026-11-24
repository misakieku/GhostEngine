namespace Ghost.D3D12;

public unsafe partial struct D3D12_MEMCPY_DEST
{
    public void* pData;

    [NativeTypeName("SIZE_T")]
    public nuint RowPitch;

    [NativeTypeName("SIZE_T")]
    public nuint SlicePitch;
}
