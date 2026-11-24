namespace Ghost.D3D12;

public unsafe partial struct D3D12_INPUT_ELEMENT_DESC
{
    [NativeTypeName("LPCSTR")]
    public sbyte* SemanticName;

    public uint SemanticIndex;

    public DXGI_FORMAT Format;

    public uint InputSlot;

    public uint AlignedByteOffset;

    public D3D12_INPUT_CLASSIFICATION InputSlotClass;

    public uint InstanceDataStepRate;
}
