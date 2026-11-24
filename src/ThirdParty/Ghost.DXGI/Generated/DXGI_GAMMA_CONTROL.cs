using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_GAMMA_CONTROL
{
    public DXGI_RGB Scale;

    public DXGI_RGB Offset;

    [NativeTypeName("DXGI_RGB[1025]")]
    public _GammaCurve_e__FixedBuffer GammaCurve;

    [InlineArray(1025)]
    public partial struct _GammaCurve_e__FixedBuffer
    {
        public DXGI_RGB e0;
    }
}
