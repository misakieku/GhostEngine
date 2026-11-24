namespace Ghost.D3D12;

public unsafe partial struct D3D12_APPLICATION_DESC
{
    [NativeTypeName("LPCWSTR")]
    public char* pExeFilename;

    [NativeTypeName("LPCWSTR")]
    public char* pName;

    public D3D12_VERSION_NUMBER Version;

    [NativeTypeName("LPCWSTR")]
    public char* pEngineName;

    public D3D12_VERSION_NUMBER EngineVersion;
}
