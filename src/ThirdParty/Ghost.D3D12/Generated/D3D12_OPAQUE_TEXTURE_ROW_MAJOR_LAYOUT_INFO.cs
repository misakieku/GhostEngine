namespace Ghost.D3D12;

public partial struct D3D12_OPAQUE_TEXTURE_ROW_MAJOR_LAYOUT_INFO
{
    [NativeTypeName("BOOL")]
    public int IsRowMajor;

    [NativeTypeName("UINT64")]
    public ulong Offset;

    [NativeTypeName("UINT64")]
    public ulong RowPitch;

    public uint Width;

    public uint Height;
}
