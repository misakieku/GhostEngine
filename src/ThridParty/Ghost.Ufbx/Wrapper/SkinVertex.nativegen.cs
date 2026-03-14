namespace Ghost.Ufbx;

public unsafe struct SkinVertex
{
    private ufbx_skin_vertex* _ptr;

    internal SkinVertex(ufbx_skin_vertex* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint WeightBegin => _ptr->weight_begin;

    public uint NumWeights => _ptr->num_weights;

    public float DqWeight => _ptr->dq_weight;

    internal ufbx_skin_vertex* GetUnsafePtr() => _ptr;
}
