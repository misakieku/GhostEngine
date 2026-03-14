namespace Ghost.Ufbx;

public unsafe struct PropOverride
{
    private ufbx_prop_override* _ptr;

    internal PropOverride(ufbx_prop_override* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint ElementId => _ptr->element_id;

    public ReadOnlySpan<byte> PropNameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->prop_name);
    public string PropName => NativeWrapperHelpers.GetString(_ptr->prop_name);

    public Misaki.HighPerformance.Mathematics.float4 Value => _ptr->value;

    public ReadOnlySpan<byte> ValueStrBytes => NativeWrapperHelpers.AsByteSpan(_ptr->value_str);
    public string ValueStr => NativeWrapperHelpers.GetString(_ptr->value_str);

    public long ValueInt => _ptr->value_int;

    internal ufbx_prop_override* GetUnsafePtr() => _ptr;
}
