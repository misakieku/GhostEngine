namespace Ghost.Win32;

public unsafe partial struct STATSTG
{
    public char* pwcsName;
    public uint type;
    public ulong cbSize;
    public FILETIME mtime;
    public FILETIME ctime;
    public FILETIME atime;
    public uint grfMode;
    public uint grfLocksSupported;
    public Guid clsid;
    public uint grfStateBits;
    public uint reserved;
}
