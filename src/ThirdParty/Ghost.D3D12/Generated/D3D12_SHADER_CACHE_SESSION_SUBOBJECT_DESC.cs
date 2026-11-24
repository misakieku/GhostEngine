namespace Ghost.D3D12;

public unsafe partial struct D3D12_SHADER_CACHE_SESSION_SUBOBJECT_DESC
{
    public ID3D12ShaderCacheSession1* pSession;

    public D3D12_SHADER_CACHE_SESSION_SUBOBJECT_FLAGS Flags;
}
