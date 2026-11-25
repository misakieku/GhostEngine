namespace Ghost.D3D12;

public partial struct D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC
{
    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong DestAccelerationStructureData;

    public D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS Inputs;

    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong SourceAccelerationStructureData;

    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong ScratchAccelerationStructureData;
}
