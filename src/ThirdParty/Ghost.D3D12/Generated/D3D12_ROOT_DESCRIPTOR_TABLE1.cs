namespace Ghost.D3D12;

public unsafe partial struct D3D12_ROOT_DESCRIPTOR_TABLE1
{
    public uint NumDescriptorRanges;

    [NativeTypeName("const D3D12_DESCRIPTOR_RANGE1 *")]
    public D3D12_DESCRIPTOR_RANGE1* pDescriptorRanges;
}
