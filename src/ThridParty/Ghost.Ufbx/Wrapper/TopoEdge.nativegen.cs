namespace Ghost.Ufbx;

public unsafe struct TopoEdge
{
    private ufbx_topo_edge* _ptr;

    internal TopoEdge(ufbx_topo_edge* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint TopoNextVertexEdge(nuint numTopo, uint index)
    {
        return Api.ufbx_topo_next_vertex_edge(_ptr, numTopo, index);
    }

    public uint TopoPrevVertexEdge(nuint numTopo, uint index)
    {
        return Api.ufbx_topo_prev_vertex_edge(_ptr, numTopo, index);
    }

    public uint Index => _ptr->index;

    public uint Next => _ptr->next;

    public uint Prev => _ptr->prev;

    public uint Twin => _ptr->twin;

    public uint Face => _ptr->face;

    public uint Edge => _ptr->edge;

    public ufbx_topo_flags Flags => _ptr->flags;

    internal ufbx_topo_edge* GetUnsafePtr() => _ptr;
}
