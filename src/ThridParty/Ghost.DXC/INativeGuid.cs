namespace Ghost.DXC;

public unsafe interface INativeGuid
{
    protected internal static abstract Guid* NativeGuid { get; }
}
