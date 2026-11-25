namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS1
{
    [NativeTypeName("BOOL")]
    public int WaveOps;

    public uint WaveLaneCountMin;

    public uint WaveLaneCountMax;

    public uint TotalLaneCount;

    [NativeTypeName("BOOL")]
    public int ExpandedComputeResourceStates;

    [NativeTypeName("BOOL")]
    public int Int64ShaderOps;
}
