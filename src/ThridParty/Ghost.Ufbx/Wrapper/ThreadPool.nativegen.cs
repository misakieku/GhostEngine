namespace Ghost.Ufbx;

public unsafe struct ThreadPool
{
    private ufbx_thread_pool* _ptr;

    internal ThreadPool(ufbx_thread_pool* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void* User => _ptr->user;

    internal ufbx_thread_pool* GetUnsafePtr() => _ptr;
}
