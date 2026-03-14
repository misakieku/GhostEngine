namespace Ghost.Ufbx;

public unsafe struct DomNode
{
    private ufbx_dom_node* _ptr;

    internal DomNode(ufbx_dom_node* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public DomNode DomFindLen(sbyte* name, nuint nameLen)
    {
        return new(Api.ufbx_dom_find_len(_ptr, name, nameLen));
    }

    public DomNode DomFind(sbyte* name)
    {
        return new(Api.ufbx_dom_find(_ptr, name));
    }

    public bool DomIsArray()
    {
        return Api.ufbx_dom_is_array(_ptr);
    }

    public nuint DomArraySize()
    {
        return Api.ufbx_dom_array_size(_ptr);
    }

    public ufbx_int32_list DomAsInt32List()
    {
        return Api.ufbx_dom_as_int32_list(_ptr);
    }

    public ufbx_int64_list DomAsInt64List()
    {
        return Api.ufbx_dom_as_int64_list(_ptr);
    }

    public ufbx_float_list DomAsFloatList()
    {
        return Api.ufbx_dom_as_float_list(_ptr);
    }

    public ufbx_double_list DomAsDoubleList()
    {
        return Api.ufbx_dom_as_double_list(_ptr);
    }

    public ufbx_real_list DomAsRealList()
    {
        return Api.ufbx_dom_as_real_list(_ptr);
    }

    public ufbx_blob_list DomAsBlobList()
    {
        return Api.ufbx_dom_as_blob_list(_ptr);
    }

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public DomNodeList Children => new(_ptr->children.data, _ptr->children.count);

    public ReadOnlySpan<ufbx_dom_value> Values => _ptr->values.data == null ? ReadOnlySpan<ufbx_dom_value>.Empty : new ReadOnlySpan<ufbx_dom_value>(_ptr->values.data, checked((int)_ptr->values.count));

    internal ufbx_dom_node* GetUnsafePtr() => _ptr;
}
