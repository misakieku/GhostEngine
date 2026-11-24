namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS15
{
    [NativeTypeName("BOOL")]
    public int TriangleFanSupported;

    [NativeTypeName("BOOL")]
    public int DynamicIndexBufferStripCutSupported;
}
