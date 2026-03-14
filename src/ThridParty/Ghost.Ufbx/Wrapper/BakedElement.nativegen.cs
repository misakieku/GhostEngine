namespace Ghost.Ufbx;

public unsafe struct BakedElement
{
    private ufbx_baked_element* _ptr;

    internal BakedElement(ufbx_baked_element* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint ElementId => _ptr->element_id;

    public ReadOnlySpan<ufbx_baked_prop> Props => _ptr->props.data == null ? ReadOnlySpan<ufbx_baked_prop>.Empty : new ReadOnlySpan<ufbx_baked_prop>(_ptr->props.data, checked((int)_ptr->props.count));

    internal ufbx_baked_element* GetUnsafePtr() => _ptr;
}
