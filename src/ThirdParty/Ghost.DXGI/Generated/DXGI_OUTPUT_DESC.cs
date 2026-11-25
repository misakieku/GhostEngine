using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_OUTPUT_DESC
{
    [NativeTypeName("WCHAR[32]")]
    public _DeviceName_e__FixedBuffer DeviceName;

    public RECT DesktopCoordinates;

    [NativeTypeName("BOOL")]
    public int AttachedToDesktop;

    public DXGI_MODE_ROTATION Rotation;

    public HMONITOR Monitor;

    [InlineArray(32)]
    public partial struct _DeviceName_e__FixedBuffer
    {
        public char e0;
    }
}
