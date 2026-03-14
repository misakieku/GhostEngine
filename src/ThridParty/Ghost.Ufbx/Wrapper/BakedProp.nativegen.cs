namespace Ghost.Ufbx;

public unsafe struct BakedProp
{
    private ufbx_baked_prop* _ptr;

    internal BakedProp(ufbx_baked_prop* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public bool ConstantValue => _ptr->constant_value;

    public ReadOnlySpan<ufbx_baked_vec3> Keys => _ptr->keys.data == null ? ReadOnlySpan<ufbx_baked_vec3>.Empty : new ReadOnlySpan<ufbx_baked_vec3>(_ptr->keys.data, checked((int)_ptr->keys.count));

    internal ufbx_baked_prop* GetUnsafePtr() => _ptr;
}
