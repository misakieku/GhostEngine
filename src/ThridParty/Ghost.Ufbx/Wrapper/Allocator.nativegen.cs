namespace Ghost.Ufbx;

public unsafe struct Allocator
{
    private ufbx_allocator* _ptr;

    internal Allocator(ufbx_allocator* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void* User => _ptr->user;

    internal ufbx_allocator* GetUnsafePtr() => _ptr;
}
