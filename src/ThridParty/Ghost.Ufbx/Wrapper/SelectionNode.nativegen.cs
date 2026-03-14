namespace Ghost.Ufbx;

public unsafe struct SelectionNode
{
    private ufbx_selection_node* _ptr;

    internal SelectionNode(ufbx_selection_node* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasTargetNode => _ptr->target_node != null;
    public Node TargetNode => _ptr->target_node != null ? new(_ptr->target_node) : throw new InvalidOperationException("TargetNode is null.");

    public bool HasTargetMesh => _ptr->target_mesh != null;
    public Mesh TargetMesh => _ptr->target_mesh != null ? new(_ptr->target_mesh) : throw new InvalidOperationException("TargetMesh is null.");

    public bool IncludeNode => _ptr->include_node;

    public ReadOnlySpan<uint> Vertices => _ptr->vertices.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->vertices.data, checked((int)_ptr->vertices.count));

    public ReadOnlySpan<uint> Edges => _ptr->edges.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->edges.data, checked((int)_ptr->edges.count));

    public ReadOnlySpan<uint> Faces => _ptr->faces.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->faces.data, checked((int)_ptr->faces.count));

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_selection_node* GetUnsafePtr() => _ptr;
}
