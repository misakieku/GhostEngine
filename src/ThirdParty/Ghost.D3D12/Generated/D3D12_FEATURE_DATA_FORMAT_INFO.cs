namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_FORMAT_INFO
{
    public DXGI_FORMAT Format;

    [NativeTypeName("UINT8")]
    public byte PlaneCount;
}
