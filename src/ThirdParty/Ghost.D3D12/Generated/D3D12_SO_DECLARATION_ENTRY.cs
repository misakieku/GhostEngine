namespace Ghost.D3D12;

public unsafe partial struct D3D12_SO_DECLARATION_ENTRY
{
    public uint Stream;

    [NativeTypeName("LPCSTR")]
    public sbyte* SemanticName;

    public uint SemanticIndex;

    public byte StartComponent;

    public byte ComponentCount;

    public byte OutputSlot;
}
