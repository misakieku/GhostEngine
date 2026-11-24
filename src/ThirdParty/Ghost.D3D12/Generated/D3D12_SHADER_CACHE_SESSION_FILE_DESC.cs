namespace Ghost.D3D12;

public unsafe partial struct D3D12_SHADER_CACHE_SESSION_FILE_DESC
{
    [NativeTypeName("const D3D12_CACHE_FILE_DESC *")]
    public D3D12_CACHE_FILE_DESC* pDescs;

    public uint NumDescs;

    public D3D12_SHADER_CACHE_SESSION_FILE_FLAGS Flags;
}
