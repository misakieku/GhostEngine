namespace Ghost.D3D12;

public unsafe partial struct D3D12_PRERASTERIZATION_OUTPUT_LINKAGE_SIGNATURE_DESC
{
    [NativeTypeName("const D3D12_PRERASTERIZATION_OUTPUT_LINKAGE_ELEMENT_DESC *")]
    public D3D12_PRERASTERIZATION_OUTPUT_LINKAGE_ELEMENT_DESC* pOutputLinkageElementDescs;

    public uint NumElements;
}
