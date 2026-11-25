namespace Ghost.D3D12;

public partial struct D3D12_PACKED_MIP_INFO
{
    [NativeTypeName("UINT8")]
    public byte NumStandardMips;

    [NativeTypeName("UINT8")]
    public byte NumPackedMips;

    public uint NumTilesForPackedMips;

    public uint StartTileIndexInOverallResource;
}
