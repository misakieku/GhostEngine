namespace Ghost.Ufbx;

public unsafe struct Progress
{
    private ufbx_progress* _ptr;

    internal Progress(ufbx_progress* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ulong BytesRead => _ptr->bytes_read;

    public ulong BytesTotal => _ptr->bytes_total;

    internal ufbx_progress* GetUnsafePtr() => _ptr;
}
