namespace Ghost.Ufbx;

public unsafe struct TessellateSurfaceOpts
{
    private ufbx_tessellate_surface_opts* _ptr;

    internal TessellateSurfaceOpts(ufbx_tessellate_surface_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AllocatorOpts TempAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->temp_allocator));

    public AllocatorOpts ResultAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->result_allocator));

    public nuint SpanSubdivisionU => _ptr->span_subdivision_u;

    public nuint SpanSubdivisionV => _ptr->span_subdivision_v;

    public bool SkipMeshParts => _ptr->skip_mesh_parts;

    internal ufbx_tessellate_surface_opts* GetUnsafePtr() => _ptr;
}
