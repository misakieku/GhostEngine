namespace Ghost.Ufbx;

public unsafe struct NameElement
{
    private ufbx_name_element* _ptr;

    internal NameElement(ufbx_name_element* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public ufbx_element_type Type => _ptr->type;

    public bool HasElement => _ptr->element != null;
    public Element Element => _ptr->element != null ? new(_ptr->element) : throw new InvalidOperationException("Element is null.");

    internal ufbx_name_element* GetUnsafePtr() => _ptr;
}
