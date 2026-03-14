namespace Ghost.Ufbx;

public unsafe struct ShaderBinding
{
    private ufbx_shader_binding* _ptr;

    internal ShaderBinding(ufbx_shader_binding* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<ufbx_shader_prop_binding> PropBindings => _ptr->prop_bindings.data == null ? ReadOnlySpan<ufbx_shader_prop_binding>.Empty : new ReadOnlySpan<ufbx_shader_prop_binding>(_ptr->prop_bindings.data, checked((int)_ptr->prop_bindings.count));

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_shader_binding* GetUnsafePtr() => _ptr;
}
