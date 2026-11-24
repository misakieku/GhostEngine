namespace Ghost.D3D12;

public unsafe partial struct D3D12_HIT_GROUP_DESC
{
    [NativeTypeName("LPCWSTR")]
    public char* HitGroupExport;

    public D3D12_HIT_GROUP_TYPE Type;

    [NativeTypeName("LPCWSTR")]
    public char* AnyHitShaderImport;

    [NativeTypeName("LPCWSTR")]
    public char* ClosestHitShaderImport;

    [NativeTypeName("LPCWSTR")]
    public char* IntersectionShaderImport;
}
