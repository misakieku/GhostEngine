namespace Ghost.Ufbx;

public unsafe struct ShaderPropBinding
{
    private ufbx_shader_prop_binding* _ptr;

    internal ShaderPropBinding(ufbx_shader_prop_binding* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> ShaderPropBytes => NativeWrapperHelpers.AsByteSpan(_ptr->shader_prop);
    public string ShaderProp => NativeWrapperHelpers.GetString(_ptr->shader_prop);

    public ReadOnlySpan<byte> MaterialPropBytes => NativeWrapperHelpers.AsByteSpan(_ptr->material_prop);
    public string MaterialProp => NativeWrapperHelpers.GetString(_ptr->material_prop);

    internal ufbx_shader_prop_binding* GetUnsafePtr() => _ptr;
}
