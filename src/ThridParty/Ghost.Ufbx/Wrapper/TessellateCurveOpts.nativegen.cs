namespace Ghost.Ufbx;

public unsafe struct TessellateCurveOpts
{
    private ufbx_tessellate_curve_opts* _ptr;

    internal TessellateCurveOpts(ufbx_tessellate_curve_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AllocatorOpts TempAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->temp_allocator));

    public AllocatorOpts ResultAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->result_allocator));

    public nuint SpanSubdivision => _ptr->span_subdivision;

    internal ufbx_tessellate_curve_opts* GetUnsafePtr() => _ptr;
}
