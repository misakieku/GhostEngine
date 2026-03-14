namespace Ghost.Ufbx;

public unsafe struct Edge
{
    private ufbx_edge* _ptr;

    internal Edge(ufbx_edge* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint A => _ptr->a;

    public uint B => _ptr->b;

    internal ufbx_edge* GetUnsafePtr() => _ptr;
}
