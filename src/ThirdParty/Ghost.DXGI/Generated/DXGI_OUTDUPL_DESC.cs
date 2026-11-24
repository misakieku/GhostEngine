namespace Ghost.DXGI;

public partial struct DXGI_OUTDUPL_DESC
{
    public DXGI_MODE_DESC ModeDesc;

    public DXGI_MODE_ROTATION Rotation;

    [NativeTypeName("BOOL")]
    public int DesktopImageInSystemMemory;
}
