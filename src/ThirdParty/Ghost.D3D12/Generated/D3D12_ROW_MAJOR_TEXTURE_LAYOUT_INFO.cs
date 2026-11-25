namespace Ghost.D3D12;

public partial struct D3D12_ROW_MAJOR_TEXTURE_LAYOUT_INFO
{
    [NativeTypeName("UINT64")]
    public ulong Offset;

    public uint RowPitch;

    public uint HeightInRows;

    [NativeTypeName("UINT64")]
    public ulong SizeInBytes;
}
