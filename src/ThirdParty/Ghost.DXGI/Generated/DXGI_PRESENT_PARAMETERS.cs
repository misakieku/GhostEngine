namespace Ghost.DXGI;

public unsafe partial struct DXGI_PRESENT_PARAMETERS
{
    public uint DirtyRectsCount;

    public RECT* pDirtyRects;

    public RECT* pScrollRect;

    public POINT* pScrollOffset;
}
