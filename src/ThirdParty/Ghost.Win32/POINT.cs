namespace Ghost.Win32;

public partial struct POINT
{
    public int x;
    public int y;

    public POINT(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}
