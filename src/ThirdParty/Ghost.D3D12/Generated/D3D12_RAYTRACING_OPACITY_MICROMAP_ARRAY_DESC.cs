namespace Ghost.D3D12;

public unsafe partial struct D3D12_RAYTRACING_OPACITY_MICROMAP_ARRAY_DESC
{
    public uint NumOmmHistogramEntries;

    [NativeTypeName("const D3D12_RAYTRACING_OPACITY_MICROMAP_HISTOGRAM_ENTRY *")]
    public D3D12_RAYTRACING_OPACITY_MICROMAP_HISTOGRAM_ENTRY* pOmmHistogram;

    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong InputBuffer;

    public D3D12_GPU_VIRTUAL_ADDRESS_AND_STRIDE PerOmmDescs;
}
