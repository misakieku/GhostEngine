namespace Ghost.D3D12;

public unsafe partial struct D3D12_TEXTURE_COPY_REGION
{
    public uint DestX;

    public uint DestY;

    public uint DestZ;

    [NativeTypeName("const D3D12_BOX *")]
    public D3D12_BOX* pSourceBox;
}
