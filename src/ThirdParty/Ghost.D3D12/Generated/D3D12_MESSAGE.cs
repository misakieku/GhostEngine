namespace Ghost.D3D12;

public unsafe partial struct D3D12_MESSAGE
{
    public D3D12_MESSAGE_CATEGORY Category;

    public D3D12_MESSAGE_SEVERITY Severity;

    public D3D12_MESSAGE_ID ID;

    [NativeTypeName("const char *")]
    public sbyte* pDescription;

    [NativeTypeName("SIZE_T")]
    public nuint DescriptionByteLength;
}
