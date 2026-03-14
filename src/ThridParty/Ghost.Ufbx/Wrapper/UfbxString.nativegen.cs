namespace Ghost.Ufbx;

public unsafe struct UfbxString
{
    private ufbx_string* _ptr;

    internal UfbxString(ufbx_string* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public sbyte* Data => _ptr->data;

    public nuint Length => _ptr->length;

    internal ufbx_string* GetUnsafePtr() => _ptr;
}
