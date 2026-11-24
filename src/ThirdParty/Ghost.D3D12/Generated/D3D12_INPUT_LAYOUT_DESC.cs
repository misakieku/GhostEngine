namespace Ghost.D3D12;

public unsafe partial struct D3D12_INPUT_LAYOUT_DESC
{
    [NativeTypeName("const D3D12_INPUT_ELEMENT_DESC *")]
    public D3D12_INPUT_ELEMENT_DESC* pInputElementDescs;

    public uint NumElements;
}
