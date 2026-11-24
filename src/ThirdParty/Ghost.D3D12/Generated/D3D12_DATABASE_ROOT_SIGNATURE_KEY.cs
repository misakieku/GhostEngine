namespace Ghost.D3D12;

public unsafe partial struct D3D12_DATABASE_ROOT_SIGNATURE_KEY
{
    [NativeTypeName("const void *")]
    public void* pKey;

    public uint KeySize;
}
