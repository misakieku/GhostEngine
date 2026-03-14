namespace Ghost.Ufbx;

public unsafe struct SkinWeight
{
    private ufbx_skin_weight* _ptr;

    internal SkinWeight(ufbx_skin_weight* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint ClusterIndex => _ptr->cluster_index;

    public float Weight => _ptr->weight;

    internal ufbx_skin_weight* GetUnsafePtr() => _ptr;
}
