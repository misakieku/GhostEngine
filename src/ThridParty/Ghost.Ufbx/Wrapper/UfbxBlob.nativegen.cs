namespace Ghost.Ufbx;

public unsafe struct UfbxBlob
{
    private ufbx_blob* _ptr;

    internal UfbxBlob(ufbx_blob* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void* Data => _ptr->data;

    public nuint Size => _ptr->size;

    internal ufbx_blob* GetUnsafePtr() => _ptr;
}
