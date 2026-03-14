namespace Ghost.Ufbx;

public unsafe struct Shader
{
    private ufbx_shader* _ptr;

    internal Shader(ufbx_shader* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public string FindShaderPropLen(sbyte* name, nuint nameLen)
    {
        return NativeWrapperHelpers.GetString(Api.ufbx_find_shader_prop_len(_ptr, name, nameLen));
    }

    public string FindShaderProp(sbyte* name)
    {
        return NativeWrapperHelpers.GetString(Api.ufbx_find_shader_prop(_ptr, name));
    }

    public ufbx_shader_prop_binding_list FindShaderPropBindingsLen(sbyte* name, nuint nameLen)
    {
        return Api.ufbx_find_shader_prop_bindings_len(_ptr, name, nameLen);
    }

    public ufbx_shader_prop_binding_list FindShaderPropBindings(sbyte* name)
    {
        return Api.ufbx_find_shader_prop_bindings(_ptr, name);
    }

    public ufbx_shader_type Type => _ptr->type;

    public ShaderBindingList Bindings => new(_ptr->bindings.data, _ptr->bindings.count);

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_shader* GetUnsafePtr() => _ptr;
}
