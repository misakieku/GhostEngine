namespace Ghost.Ufbx;

public unsafe struct ThreadOpts
{
    private ufbx_thread_opts* _ptr;

    internal ThreadOpts(ufbx_thread_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ThreadPool Pool => new((ufbx_thread_pool*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->pool));

    public nuint NumTasks => _ptr->num_tasks;

    public nuint MemoryLimit => _ptr->memory_limit;

    internal ufbx_thread_opts* GetUnsafePtr() => _ptr;
}
