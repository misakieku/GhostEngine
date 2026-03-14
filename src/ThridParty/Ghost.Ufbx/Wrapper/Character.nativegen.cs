namespace Ghost.Ufbx;

public unsafe struct Character
{
    private ufbx_character* _ptr;

    internal Character(ufbx_character* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_character* GetUnsafePtr() => _ptr;
}
