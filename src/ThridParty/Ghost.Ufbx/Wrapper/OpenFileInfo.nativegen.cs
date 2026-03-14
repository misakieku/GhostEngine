namespace Ghost.Ufbx;

public unsafe struct OpenFileInfo
{
    private ufbx_open_file_info* _ptr;

    internal OpenFileInfo(ufbx_open_file_info* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public nuint Context => _ptr->context;

    public ufbx_open_file_type Type => _ptr->type;

    public ReadOnlySpan<byte> OriginalFilename => NativeWrapperHelpers.AsSpan(_ptr->original_filename);

    internal ufbx_open_file_info* GetUnsafePtr() => _ptr;
}
