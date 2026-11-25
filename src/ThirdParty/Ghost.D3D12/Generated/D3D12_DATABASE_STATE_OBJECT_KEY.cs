namespace Ghost.D3D12;

public unsafe partial struct D3D12_DATABASE_STATE_OBJECT_KEY
{
    [NativeTypeName("const void *")]
    public void* pKey;

    public uint KeySize;
}
