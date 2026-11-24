namespace Ghost.D3D12;

public unsafe partial struct D3D12_OUTPUT_LINKAGE_ELEMENT_DESC
{
    [NativeTypeName("LPCSTR")]
    public sbyte* SemanticName;

    public uint SemanticIndex;

    public byte StartComponent;

    public byte ComponentCount;
}
