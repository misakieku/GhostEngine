namespace Ghost.Ufbx;

public unsafe struct OpenMemoryOpts
{
    private ufbx_open_memory_opts* _ptr;

    internal OpenMemoryOpts(ufbx_open_memory_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AllocatorOpts Allocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->allocator));

    public bool NoCopy => _ptr->no_copy;

    public CloseMemoryCb CloseCb => new((ufbx_close_memory_cb*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->close_cb));

    internal ufbx_open_memory_opts* GetUnsafePtr() => _ptr;
}
