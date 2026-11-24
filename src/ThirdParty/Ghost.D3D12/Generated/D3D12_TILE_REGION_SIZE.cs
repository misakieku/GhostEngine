namespace Ghost.D3D12;

public partial struct D3D12_TILE_REGION_SIZE
{
    public uint NumTiles;

    [NativeTypeName("BOOL")]
    public int UseBox;

    public uint Width;

    [NativeTypeName("UINT16")]
    public ushort Height;

    [NativeTypeName("UINT16")]
    public ushort Depth;
}
