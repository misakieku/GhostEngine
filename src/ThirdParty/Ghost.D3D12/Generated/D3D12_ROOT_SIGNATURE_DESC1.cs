namespace Ghost.D3D12;

public unsafe partial struct D3D12_ROOT_SIGNATURE_DESC1
{
    public uint NumParameters;

    [NativeTypeName("const D3D12_ROOT_PARAMETER1 *")]
    public D3D12_ROOT_PARAMETER1* pParameters;

    public uint NumStaticSamplers;

    [NativeTypeName("const D3D12_STATIC_SAMPLER_DESC *")]
    public D3D12_STATIC_SAMPLER_DESC* pStaticSamplers;

    public D3D12_ROOT_SIGNATURE_FLAGS Flags;
}
