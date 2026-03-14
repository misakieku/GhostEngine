namespace Ghost.Ufbx;

public unsafe struct ErrorFrame
{
    private ufbx_error_frame* _ptr;

    internal ErrorFrame(ufbx_error_frame* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint SourceLine => _ptr->source_line;

    public ReadOnlySpan<byte> FunctionBytes => NativeWrapperHelpers.AsByteSpan(_ptr->function);
    public string Function => NativeWrapperHelpers.GetString(_ptr->function);

    public ReadOnlySpan<byte> DescriptionBytes => NativeWrapperHelpers.AsByteSpan(_ptr->description);
    public string Description => NativeWrapperHelpers.GetString(_ptr->description);

    internal ufbx_error_frame* GetUnsafePtr() => _ptr;
}
