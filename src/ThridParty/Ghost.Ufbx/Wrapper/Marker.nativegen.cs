namespace Ghost.Ufbx;

public unsafe struct Marker
{
    private ufbx_marker* _ptr;

    internal Marker(ufbx_marker* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_marker_type Type => _ptr->type;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_marker* GetUnsafePtr() => _ptr;
}
