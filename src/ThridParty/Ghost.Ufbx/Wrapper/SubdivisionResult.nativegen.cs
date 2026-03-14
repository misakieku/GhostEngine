namespace Ghost.Ufbx;

public unsafe struct SubdivisionResult
{
    private ufbx_subdivision_result* _ptr;

    internal SubdivisionResult(ufbx_subdivision_result* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public nuint ResultMemoryUsed => _ptr->result_memory_used;

    public nuint TempMemoryUsed => _ptr->temp_memory_used;

    public nuint ResultAllocs => _ptr->result_allocs;

    public nuint TempAllocs => _ptr->temp_allocs;

    public ReadOnlySpan<ufbx_subdivision_weight_range> SourceVertexRanges => _ptr->source_vertex_ranges.data == null ? ReadOnlySpan<ufbx_subdivision_weight_range>.Empty : new ReadOnlySpan<ufbx_subdivision_weight_range>(_ptr->source_vertex_ranges.data, checked((int)_ptr->source_vertex_ranges.count));

    public ReadOnlySpan<ufbx_subdivision_weight> SourceVertexWeights => _ptr->source_vertex_weights.data == null ? ReadOnlySpan<ufbx_subdivision_weight>.Empty : new ReadOnlySpan<ufbx_subdivision_weight>(_ptr->source_vertex_weights.data, checked((int)_ptr->source_vertex_weights.count));

    public ReadOnlySpan<ufbx_subdivision_weight_range> SkinClusterRanges => _ptr->skin_cluster_ranges.data == null ? ReadOnlySpan<ufbx_subdivision_weight_range>.Empty : new ReadOnlySpan<ufbx_subdivision_weight_range>(_ptr->skin_cluster_ranges.data, checked((int)_ptr->skin_cluster_ranges.count));

    public ReadOnlySpan<ufbx_subdivision_weight> SkinClusterWeights => _ptr->skin_cluster_weights.data == null ? ReadOnlySpan<ufbx_subdivision_weight>.Empty : new ReadOnlySpan<ufbx_subdivision_weight>(_ptr->skin_cluster_weights.data, checked((int)_ptr->skin_cluster_weights.count));

    internal ufbx_subdivision_result* GetUnsafePtr() => _ptr;
}
