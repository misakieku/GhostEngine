namespace Ghost.D3D12;

public unsafe partial struct D3D12_SHADER_CACHE_SESSION_DISK_DESC
{
    public Guid Identifier;

    public D3D12_SHADER_CACHE_FLAGS Flags;

    [NativeTypeName("LPCWSTR")]
    public char* pDirectory;

    public uint MaximumInMemoryCacheSizeBytes;

    public uint MaximumInMemoryCacheEntries;

    [NativeTypeName("UINT64")]
    public ulong MaximumValueFileSizeBytes;

    [NativeTypeName("UINT64")]
    public ulong Version;
}
