namespace Ghost.Win32;

public unsafe interface INativeGuid
{
    static abstract Guid* NativeGuid { get; }
}
