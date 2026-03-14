namespace Ghost.Ufbx;

public unsafe struct ProgressCb
{
    private ufbx_progress_cb* _ptr;

    internal ProgressCb(ufbx_progress_cb* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void* User => _ptr->user;

    internal ufbx_progress_cb* GetUnsafePtr() => _ptr;
}
