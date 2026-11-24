namespace Ghost.D3D12;

public unsafe partial struct D3D12_TRIM_NOTIFICATION
{
    public void* pContext;

    public D3D12_TRIM_NOTIFICATION_FLAGS Flags;

    [NativeTypeName("UINT64")]
    public ulong NumBytesToTrim;
}
