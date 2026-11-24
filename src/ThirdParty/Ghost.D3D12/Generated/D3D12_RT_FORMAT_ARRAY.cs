using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public partial struct D3D12_RT_FORMAT_ARRAY
{
    [NativeTypeName("DXGI_FORMAT[8]")]
    public _RTFormats_e__FixedBuffer RTFormats;

    public uint NumRenderTargets;

    [InlineArray(8)]
    public partial struct _RTFormats_e__FixedBuffer
    {
        public DXGI_FORMAT e0;
    }
}
