namespace Ghost.D3D12;

public unsafe partial struct D3D12_MULTI_NODE_CPU_INPUT
{
    public uint NumNodeInputs;

    [NativeTypeName("const D3D12_NODE_CPU_INPUT *")]
    public D3D12_NODE_CPU_INPUT* pNodeInputs;

    [NativeTypeName("UINT64")]
    public ulong NodeInputStrideInBytes;
}
