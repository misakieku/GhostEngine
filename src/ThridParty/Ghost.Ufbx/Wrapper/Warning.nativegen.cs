namespace Ghost.Ufbx;

public unsafe struct Warning
{
    private ufbx_warning* _ptr;

    internal Warning(ufbx_warning* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_warning_type Type => _ptr->type;

    public ReadOnlySpan<byte> DescriptionBytes => NativeWrapperHelpers.AsByteSpan(_ptr->description);
    public string Description => NativeWrapperHelpers.GetString(_ptr->description);

    public uint ElementId => _ptr->element_id;

    public nuint Count => _ptr->count;

    internal ufbx_warning* GetUnsafePtr() => _ptr;
}
