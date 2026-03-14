namespace Ghost.Ufbx;

public unsafe struct EvaluateOpts
{
    private ufbx_evaluate_opts* _ptr;

    internal EvaluateOpts(ufbx_evaluate_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AllocatorOpts TempAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->temp_allocator));

    public AllocatorOpts ResultAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->result_allocator));

    public bool EvaluateSkinning => _ptr->evaluate_skinning;

    public bool EvaluateCaches => _ptr->evaluate_caches;

    public uint EvaluateFlags => _ptr->evaluate_flags;

    public bool LoadExternalFiles => _ptr->load_external_files;

    public OpenFileCb OpenFileCb => new((ufbx_open_file_cb*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->open_file_cb));

    internal ufbx_evaluate_opts* GetUnsafePtr() => _ptr;
}
