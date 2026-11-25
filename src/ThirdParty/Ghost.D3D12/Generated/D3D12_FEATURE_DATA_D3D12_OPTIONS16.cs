namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_D3D12_OPTIONS16
{
    [NativeTypeName("BOOL")]
    public int DynamicDepthBiasSupported;

    [NativeTypeName("BOOL")]
    public int GPUUploadHeapSupported;
}
