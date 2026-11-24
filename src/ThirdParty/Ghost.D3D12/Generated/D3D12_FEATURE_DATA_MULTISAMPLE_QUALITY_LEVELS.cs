namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_MULTISAMPLE_QUALITY_LEVELS
{
    public DXGI_FORMAT Format;

    public uint SampleCount;

    public D3D12_MULTISAMPLE_QUALITY_LEVEL_FLAGS Flags;

    public uint NumQualityLevels;
}
