namespace Ghost.Ufbx;

public unsafe struct SubdivisionWeight
{
    private ufbx_subdivision_weight* _ptr;

    internal SubdivisionWeight(ufbx_subdivision_weight* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float Weight => _ptr->weight;

    public uint Index => _ptr->index;

    internal ufbx_subdivision_weight* GetUnsafePtr() => _ptr;
}
