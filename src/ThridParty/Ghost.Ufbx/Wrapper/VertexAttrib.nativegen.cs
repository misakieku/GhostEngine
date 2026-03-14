namespace Ghost.Ufbx;

public unsafe struct VertexAttrib
{
    private ufbx_vertex_attrib* _ptr;

    internal VertexAttrib(ufbx_vertex_attrib* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool Exists => _ptr->exists;

    public VoidList Values => new(_ptr->values.data, _ptr->values.count);

    public ReadOnlySpan<uint> Indices => _ptr->indices.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->indices.data, checked((int)_ptr->indices.count));

    public nuint ValueReals => _ptr->value_reals;

    public bool UniquePerVertex => _ptr->unique_per_vertex;

    public ReadOnlySpan<float> ValuesW => _ptr->values_w.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->values_w.data, checked((int)_ptr->values_w.count));

    internal ufbx_vertex_attrib* GetUnsafePtr() => _ptr;
}
