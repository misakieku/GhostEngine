namespace Ghost.D3D12;

public unsafe partial struct D3D12_CACHE_FILE_DESC
{
    public D3D12_CACHE_VALUE_TYPE_FLAGS Types;

    [NativeTypeName("UINT64")]
    public ulong MaximumSizeBytes;

    [NativeTypeName("LPCWSTR")]
    public char* pPath;
}
