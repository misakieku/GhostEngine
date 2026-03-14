namespace Ghost.Ufbx;

public unsafe struct DomValue
{
    private ufbx_dom_value* _ptr;

    internal DomValue(ufbx_dom_value* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_dom_value_type Type => _ptr->type;

    public ReadOnlySpan<byte> ValueStrBytes => NativeWrapperHelpers.AsByteSpan(_ptr->value_str);
    public string ValueStr => NativeWrapperHelpers.GetString(_ptr->value_str);

    public ReadOnlySpan<byte> ValueBlob => NativeWrapperHelpers.AsSpan(_ptr->value_blob);

    public long ValueInt => _ptr->value_int;

    public double ValueFloat => _ptr->value_float;

    internal ufbx_dom_value* GetUnsafePtr() => _ptr;
}
