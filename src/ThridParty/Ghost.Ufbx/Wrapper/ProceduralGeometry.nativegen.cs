namespace Ghost.Ufbx;

public unsafe struct ProceduralGeometry
{
    private ufbx_procedural_geometry* _ptr;

    internal ProceduralGeometry(ufbx_procedural_geometry* ptr)
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

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_procedural_geometry* GetUnsafePtr() => _ptr;
}
