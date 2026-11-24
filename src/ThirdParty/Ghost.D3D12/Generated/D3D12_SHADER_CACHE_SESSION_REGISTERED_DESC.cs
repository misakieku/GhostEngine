namespace Ghost.D3D12;

public unsafe partial struct D3D12_SHADER_CACHE_SESSION_REGISTERED_DESC
{
    [NativeTypeName("LPCWSTR")]
    public char* pComponentName;

    public D3D12_SHADER_CACHE_SESSION_REGISTERED_FLAGS Flags;
}
