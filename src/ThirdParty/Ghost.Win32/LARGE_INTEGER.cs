using System.Runtime.InteropServices;

namespace Ghost.Win32;

[StructLayout(LayoutKind.Explicit)]
public partial struct LARGE_INTEGER
{
    [FieldOffset(0)]
    public uint LowPart;

    [FieldOffset(4)]
    public int HighPart;

    [FieldOffset(0)]
    public long QuadPart;
}
