namespace Ghost.D3D12;

public unsafe partial struct D3D12_BROADCASTING_LAUNCH_OVERRIDES
{
    [NativeTypeName("const UINT *")]
    public uint* pLocalRootArgumentsTableIndex;

    [NativeTypeName("const BOOL *")]
    public int* pProgramEntry;

    [NativeTypeName("const D3D12_NODE_ID *")]
    public D3D12_NODE_ID* pNewName;

    [NativeTypeName("const D3D12_NODE_ID *")]
    public D3D12_NODE_ID* pShareInputOf;

    [NativeTypeName("const UINT *")]
    public uint* pDispatchGrid;

    [NativeTypeName("const UINT *")]
    public uint* pMaxDispatchGrid;

    public uint NumOutputOverrides;

    [NativeTypeName("const D3D12_NODE_OUTPUT_OVERRIDES *")]
    public D3D12_NODE_OUTPUT_OVERRIDES* pOutputOverrides;
}
