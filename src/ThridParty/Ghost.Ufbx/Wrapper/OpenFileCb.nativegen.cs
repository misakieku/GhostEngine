namespace Ghost.Ufbx;

public unsafe struct OpenFileCb
{
    private ufbx_open_file_cb* _ptr;

    internal OpenFileCb(ufbx_open_file_cb* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void* User => _ptr->user;

    internal ufbx_open_file_cb* GetUnsafePtr() => _ptr;
}
