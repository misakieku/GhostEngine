namespace Ghost.Ufbx;

public unsafe struct SelectionSet
{
    private ufbx_selection_set* _ptr;

    internal SelectionSet(ufbx_selection_set* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public SelectionNodeList Nodes => new(_ptr->nodes.data, _ptr->nodes.count);

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_selection_set* GetUnsafePtr() => _ptr;
}
