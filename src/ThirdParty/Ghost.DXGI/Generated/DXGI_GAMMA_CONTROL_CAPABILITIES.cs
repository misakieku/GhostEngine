using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_GAMMA_CONTROL_CAPABILITIES
{
    [NativeTypeName("BOOL")]
    public int ScaleAndOffsetSupported;

    public float MaxConvertedValue;

    public float MinConvertedValue;

    public uint NumGammaControlPoints;

    [NativeTypeName("float[1025]")]
    public _ControlPointPositions_e__FixedBuffer ControlPointPositions;

    [InlineArray(1025)]
    public partial struct _ControlPointPositions_e__FixedBuffer
    {
        public float e0;
    }
}
