namespace Ghost.D3D12;

public unsafe partial struct D3D12_META_COMMAND_DESC
{
    public Guid Id;

    [NativeTypeName("LPCWSTR")]
    public char* Name;

    public D3D12_GRAPHICS_STATES InitializationDirtyState;

    public D3D12_GRAPHICS_STATES ExecutionDirtyState;
}
