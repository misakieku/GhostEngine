namespace Ghost.D3D12;

public unsafe partial struct D3D12_NODE_OUTPUT_OVERRIDES
{
    public uint OutputIndex;

    [NativeTypeName("const D3D12_NODE_ID *")]
    public D3D12_NODE_ID* pNewName;

    [NativeTypeName("const BOOL *")]
    public int* pAllowSparseNodes;

    [NativeTypeName("const UINT *")]
    public uint* pMaxRecords;

    [NativeTypeName("const UINT *")]
    public uint* pMaxRecordsSharedWithOutputIndex;
}
