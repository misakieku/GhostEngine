namespace Ghost.StbI;

public unsafe partial struct stbi_io_callbacks
{
    [NativeTypeName("int (*)(void *, char *, int)")]
    public delegate* unmanaged[Cdecl]<void*, sbyte*, int, int> read;

    [NativeTypeName("void (*)(void *, int)")]
    public delegate* unmanaged[Cdecl]<void*, int, void> skip;

    [NativeTypeName("int (*)(void *)")]
    public delegate* unmanaged[Cdecl]<void*, int> eof;
}
