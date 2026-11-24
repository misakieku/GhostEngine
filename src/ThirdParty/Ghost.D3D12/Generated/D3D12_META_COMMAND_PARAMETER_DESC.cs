namespace Ghost.D3D12;

public unsafe partial struct D3D12_META_COMMAND_PARAMETER_DESC
{
    [NativeTypeName("LPCWSTR")]
    public char* Name;

    public D3D12_META_COMMAND_PARAMETER_TYPE Type;

    public D3D12_META_COMMAND_PARAMETER_FLAGS Flags;

    public D3D12_RESOURCE_STATES RequiredResourceState;

    public uint StructureOffset;
}
