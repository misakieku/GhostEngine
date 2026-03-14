namespace Ghost.Ufbx;

public unsafe struct LineSegment
{
    private ufbx_line_segment* _ptr;

    internal LineSegment(ufbx_line_segment* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint IndexBegin => _ptr->index_begin;

    public uint NumIndices => _ptr->num_indices;

    internal ufbx_line_segment* GetUnsafePtr() => _ptr;
}
