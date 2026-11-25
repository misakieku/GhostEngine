namespace Ghost.D3D12;

public unsafe partial struct D3D12_ROOT_DESCRIPTOR_TABLE
{
    public uint NumDescriptorRanges;

    [NativeTypeName("const D3D12_DESCRIPTOR_RANGE *")]
    public D3D12_DESCRIPTOR_RANGE* pDescriptorRanges;
}
