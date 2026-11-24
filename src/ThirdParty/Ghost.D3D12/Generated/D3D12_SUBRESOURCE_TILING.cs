namespace Ghost.D3D12;

public partial struct D3D12_SUBRESOURCE_TILING
{
    public uint WidthInTiles;

    [NativeTypeName("UINT16")]
    public ushort HeightInTiles;

    [NativeTypeName("UINT16")]
    public ushort DepthInTiles;

    public uint StartTileIndexInOverallResource;
}
