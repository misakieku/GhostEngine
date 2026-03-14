namespace Ghost.Ufbx;

public unsafe struct SkinDeformer
{
    private ufbx_skin_deformer* _ptr;

    internal SkinDeformer(ufbx_skin_deformer* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_skinning_method SkinningMethod => _ptr->skinning_method;

    public SkinClusterList Clusters => new(_ptr->clusters.data, _ptr->clusters.count);

    public ReadOnlySpan<ufbx_skin_vertex> Vertices => _ptr->vertices.data == null ? ReadOnlySpan<ufbx_skin_vertex>.Empty : new ReadOnlySpan<ufbx_skin_vertex>(_ptr->vertices.data, checked((int)_ptr->vertices.count));

    public ReadOnlySpan<ufbx_skin_weight> Weights => _ptr->weights.data == null ? ReadOnlySpan<ufbx_skin_weight>.Empty : new ReadOnlySpan<ufbx_skin_weight>(_ptr->weights.data, checked((int)_ptr->weights.count));

    public nuint MaxWeightsPerVertex => _ptr->max_weights_per_vertex;

    public nuint NumDqWeights => _ptr->num_dq_weights;

    public ReadOnlySpan<uint> DqVertices => _ptr->dq_vertices.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->dq_vertices.data, checked((int)_ptr->dq_vertices.count));

    public ReadOnlySpan<float> DqWeights => _ptr->dq_weights.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->dq_weights.data, checked((int)_ptr->dq_weights.count));

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_skin_deformer* GetUnsafePtr() => _ptr;
}
