namespace Ghost.Ufbx;

public unsafe struct VertexStream
{
    private ufbx_vertex_stream* _ptr;

    internal VertexStream(ufbx_vertex_stream* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public nuint GenerateIndices(nuint numStreams, uint* indices, nuint numIndices, AllocatorOpts allocator, Error error)
    {
        return Api.ufbx_generate_indices(_ptr, numStreams, indices, numIndices, allocator.GetUnsafePtr(), error.GetUnsafePtr());
    }

    public void* Data => _ptr->data;

    public nuint VertexCount => _ptr->vertex_count;

    public nuint VertexSize => _ptr->vertex_size;

    internal ufbx_vertex_stream* GetUnsafePtr() => _ptr;
}
