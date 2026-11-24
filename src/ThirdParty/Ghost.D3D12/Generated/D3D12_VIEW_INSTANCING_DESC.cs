namespace Ghost.D3D12;

public unsafe partial struct D3D12_VIEW_INSTANCING_DESC
{
    public uint ViewInstanceCount;

    [NativeTypeName("const D3D12_VIEW_INSTANCE_LOCATION *")]
    public D3D12_VIEW_INSTANCE_LOCATION* pViewInstanceLocations;

    public D3D12_VIEW_INSTANCING_FLAGS Flags;
}
