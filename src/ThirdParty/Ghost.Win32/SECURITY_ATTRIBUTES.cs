namespace Ghost.Win32;

public unsafe partial struct SECURITY_ATTRIBUTES
{
    public uint nLength;
    public void* lpSecurityDescriptor;
    public int bInheritHandle;
}
