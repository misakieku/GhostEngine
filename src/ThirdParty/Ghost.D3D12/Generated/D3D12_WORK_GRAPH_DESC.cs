namespace Ghost.D3D12;

public unsafe partial struct D3D12_WORK_GRAPH_DESC
{
    [NativeTypeName("LPCWSTR")]
    public char* ProgramName;

    public D3D12_WORK_GRAPH_FLAGS Flags;

    public uint NumEntrypoints;

    [NativeTypeName("const D3D12_NODE_ID *")]
    public D3D12_NODE_ID* pEntrypoints;

    public uint NumExplicitlyDefinedNodes;

    [NativeTypeName("const D3D12_NODE *")]
    public D3D12_NODE* pExplicitlyDefinedNodes;
}
