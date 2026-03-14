namespace Ghost.Ufbx;

public unsafe struct CloseMemoryCb
{
    private ufbx_close_memory_cb* _ptr;

    internal CloseMemoryCb(ufbx_close_memory_cb* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void* User => _ptr->user;

    internal ufbx_close_memory_cb* GetUnsafePtr() => _ptr;
}
