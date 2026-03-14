namespace Ghost.Ufbx;

public unsafe struct Unknown
{
    private ufbx_unknown* _ptr;

    internal Unknown(ufbx_unknown* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> TypeBytes => NativeWrapperHelpers.AsByteSpan(_ptr->type);
    public string Type => NativeWrapperHelpers.GetString(_ptr->type);

    public ReadOnlySpan<byte> SuperTypeBytes => NativeWrapperHelpers.AsByteSpan(_ptr->super_type);
    public string SuperType => NativeWrapperHelpers.GetString(_ptr->super_type);

    public ReadOnlySpan<byte> SubTypeBytes => NativeWrapperHelpers.AsByteSpan(_ptr->sub_type);
    public string SubType => NativeWrapperHelpers.GetString(_ptr->sub_type);

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_unknown* GetUnsafePtr() => _ptr;
}
