using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public partial struct D3D12_SAMPLER_DESC
{
    public D3D12_FILTER Filter;

    public D3D12_TEXTURE_ADDRESS_MODE AddressU;

    public D3D12_TEXTURE_ADDRESS_MODE AddressV;

    public D3D12_TEXTURE_ADDRESS_MODE AddressW;

    public float MipLODBias;

    public uint MaxAnisotropy;

    public D3D12_COMPARISON_FUNC ComparisonFunc;

    [NativeTypeName("FLOAT[4]")]
    public _BorderColor_e__FixedBuffer BorderColor;

    public float MinLOD;

    public float MaxLOD;

    [InlineArray(4)]
    public partial struct _BorderColor_e__FixedBuffer
    {
        public float e0;
    }
}
