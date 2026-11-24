namespace Ghost.D3D12;

public unsafe partial struct D3D12_FEATURE_DATA_GUID_TEXTURE_LAYOUT
{
    [NativeTypeName("const D3D12_RESOURCE_DESC2 *")]
    public D3D12_RESOURCE_DESC2* pDesc;

    public D3D12_FORMAT_SUPPORT1 Support1;

    public D3D12_FORMAT_SUPPORT2 Support2;

    public D3D12_GUID_TEXTURE_LAYOUT_TIER GuidTextureLayoutTier;

    [NativeTypeName("BOOL")]
    public int GuidSupported;
}
