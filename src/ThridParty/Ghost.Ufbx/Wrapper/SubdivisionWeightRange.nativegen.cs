namespace Ghost.Ufbx;

public unsafe struct SubdivisionWeightRange
{
    private ufbx_subdivision_weight_range* _ptr;

    internal SubdivisionWeightRange(ufbx_subdivision_weight_range* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint WeightBegin => _ptr->weight_begin;

    public uint NumWeights => _ptr->num_weights;

    internal ufbx_subdivision_weight_range* GetUnsafePtr() => _ptr;
}
