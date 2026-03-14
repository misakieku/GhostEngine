namespace Ghost.Ufbx;

public unsafe struct ThreadPoolInfo
{
    private ufbx_thread_pool_info* _ptr;

    internal ThreadPoolInfo(ufbx_thread_pool_info* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint MaxConcurrentTasks => _ptr->max_concurrent_tasks;

    internal ufbx_thread_pool_info* GetUnsafePtr() => _ptr;
}
