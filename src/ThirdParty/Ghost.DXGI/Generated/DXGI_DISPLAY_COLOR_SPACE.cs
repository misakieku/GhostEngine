using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_DISPLAY_COLOR_SPACE
{
    [NativeTypeName("FLOAT[8][2]")]
    public _PrimaryCoordinates_e__FixedBuffer PrimaryCoordinates;

    [NativeTypeName("FLOAT[16][2]")]
    public _WhitePoints_e__FixedBuffer WhitePoints;

    [InlineArray(8 * 2)]
    public partial struct _PrimaryCoordinates_e__FixedBuffer
    {
        public float e0_0;
    }

    [InlineArray(16 * 2)]
    public partial struct _WhitePoints_e__FixedBuffer
    {
        public float e0_0;
    }
}
