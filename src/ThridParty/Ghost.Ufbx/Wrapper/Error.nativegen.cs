namespace Ghost.Ufbx;

public unsafe struct Error
{
    private ufbx_error* _ptr;

    internal Error(ufbx_error* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_error_type Type => _ptr->type;

    public ReadOnlySpan<byte> DescriptionBytes => NativeWrapperHelpers.AsByteSpan(_ptr->description);
    public string Description => NativeWrapperHelpers.GetString(_ptr->description);

    public uint StackSize => _ptr->stack_size;

    public nuint InfoLength => _ptr->info_length;

    internal ufbx_error* GetUnsafePtr() => _ptr;
}
