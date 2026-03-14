namespace Ghost.Ufbx;

public unsafe struct InflateRetain
{
    private ufbx_inflate_retain* _ptr;

    internal InflateRetain(ufbx_inflate_retain* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool Initialized => _ptr->initialized;

    internal ufbx_inflate_retain* GetUnsafePtr() => _ptr;
}
