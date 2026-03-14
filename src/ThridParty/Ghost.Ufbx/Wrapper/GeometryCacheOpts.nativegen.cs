namespace Ghost.Ufbx;

public unsafe struct GeometryCacheOpts
{
    private ufbx_geometry_cache_opts* _ptr;

    internal GeometryCacheOpts(ufbx_geometry_cache_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AllocatorOpts TempAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->temp_allocator));

    public AllocatorOpts ResultAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->result_allocator));

    public OpenFileCb OpenFileCb => new((ufbx_open_file_cb*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->open_file_cb));

    public double FramesPerSecond => _ptr->frames_per_second;

    public ufbx_mirror_axis MirrorAxis => _ptr->mirror_axis;

    public bool UseScaleFactor => _ptr->use_scale_factor;

    public float ScaleFactor => _ptr->scale_factor;

    internal ufbx_geometry_cache_opts* GetUnsafePtr() => _ptr;
}
