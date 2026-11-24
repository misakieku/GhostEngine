namespace Ghost.D3D12;

public partial struct D3D12_QUERY_DATA_PIPELINE_STATISTICS1
{
    [NativeTypeName("UINT64")]
    public ulong IAVertices;

    [NativeTypeName("UINT64")]
    public ulong IAPrimitives;

    [NativeTypeName("UINT64")]
    public ulong VSInvocations;

    [NativeTypeName("UINT64")]
    public ulong GSInvocations;

    [NativeTypeName("UINT64")]
    public ulong GSPrimitives;

    [NativeTypeName("UINT64")]
    public ulong CInvocations;

    [NativeTypeName("UINT64")]
    public ulong CPrimitives;

    [NativeTypeName("UINT64")]
    public ulong PSInvocations;

    [NativeTypeName("UINT64")]
    public ulong HSInvocations;

    [NativeTypeName("UINT64")]
    public ulong DSInvocations;

    [NativeTypeName("UINT64")]
    public ulong CSInvocations;

    [NativeTypeName("UINT64")]
    public ulong ASInvocations;

    [NativeTypeName("UINT64")]
    public ulong MSInvocations;

    [NativeTypeName("UINT64")]
    public ulong MSPrimitives;
}
