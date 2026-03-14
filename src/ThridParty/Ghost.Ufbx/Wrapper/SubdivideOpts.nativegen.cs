namespace Ghost.Ufbx;

public unsafe struct SubdivideOpts
{
    private ufbx_subdivide_opts* _ptr;

    internal SubdivideOpts(ufbx_subdivide_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AllocatorOpts TempAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->temp_allocator));

    public AllocatorOpts ResultAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->result_allocator));

    public ufbx_subdivision_boundary Boundary => _ptr->boundary;

    public ufbx_subdivision_boundary UvBoundary => _ptr->uv_boundary;

    public bool IgnoreNormals => _ptr->ignore_normals;

    public bool InterpolateNormals => _ptr->interpolate_normals;

    public bool InterpolateTangents => _ptr->interpolate_tangents;

    public bool EvaluateSourceVertices => _ptr->evaluate_source_vertices;

    public nuint MaxSourceVertices => _ptr->max_source_vertices;

    public bool EvaluateSkinWeights => _ptr->evaluate_skin_weights;

    public nuint MaxSkinWeights => _ptr->max_skin_weights;

    public nuint SkinDeformerIndex => _ptr->skin_deformer_index;

    internal ufbx_subdivide_opts* GetUnsafePtr() => _ptr;
}
