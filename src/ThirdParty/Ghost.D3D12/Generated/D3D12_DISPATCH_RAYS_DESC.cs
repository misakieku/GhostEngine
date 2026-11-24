namespace Ghost.D3D12;

public partial struct D3D12_DISPATCH_RAYS_DESC
{
    public D3D12_GPU_VIRTUAL_ADDRESS_RANGE RayGenerationShaderRecord;

    public D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE MissShaderTable;

    public D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE HitGroupTable;

    public D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE CallableShaderTable;

    public uint Width;

    public uint Height;

    public uint Depth;
}
