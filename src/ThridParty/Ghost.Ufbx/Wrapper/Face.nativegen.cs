namespace Ghost.Ufbx;

public unsafe struct Face
{
    private ufbx_face* _ptr;

    internal Face(ufbx_face* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint IndexBegin => _ptr->index_begin;

    public uint NumIndices => _ptr->num_indices;

    internal ufbx_face* GetUnsafePtr() => _ptr;
}
