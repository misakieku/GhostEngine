namespace Ghost.Ufbx;

public unsafe struct AllocatorOpts
{
    private ufbx_allocator_opts* _ptr;

    internal AllocatorOpts(ufbx_allocator_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Allocator Allocator => new((ufbx_allocator*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->allocator));

    public nuint MemoryLimit => _ptr->memory_limit;

    public nuint AllocationLimit => _ptr->allocation_limit;

    public nuint HugeThreshold => _ptr->huge_threshold;

    public nuint MaxChunkSize => _ptr->max_chunk_size;

    internal ufbx_allocator_opts* GetUnsafePtr() => _ptr;
}
