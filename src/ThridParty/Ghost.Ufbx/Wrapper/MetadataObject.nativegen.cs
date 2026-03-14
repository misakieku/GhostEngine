namespace Ghost.Ufbx;

public unsafe struct MetadataObject
{
    private ufbx_metadata_object* _ptr;

    internal MetadataObject(ufbx_metadata_object* ptr)
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

    internal ufbx_metadata_object* GetUnsafePtr() => _ptr;
}
