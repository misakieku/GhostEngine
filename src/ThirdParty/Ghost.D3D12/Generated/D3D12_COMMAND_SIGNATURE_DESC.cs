namespace Ghost.D3D12;

public unsafe partial struct D3D12_COMMAND_SIGNATURE_DESC
{
    public uint ByteStride;

    public uint NumArgumentDescs;

    [NativeTypeName("const D3D12_INDIRECT_ARGUMENT_DESC *")]
    public D3D12_INDIRECT_ARGUMENT_DESC* pArgumentDescs;

    public uint NodeMask;
}
