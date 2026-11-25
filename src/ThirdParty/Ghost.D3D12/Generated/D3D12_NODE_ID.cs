namespace Ghost.D3D12;

public unsafe partial struct D3D12_NODE_ID
{
    [NativeTypeName("LPCWSTR")]
    public char* Name;

    public uint ArrayIndex;
}
