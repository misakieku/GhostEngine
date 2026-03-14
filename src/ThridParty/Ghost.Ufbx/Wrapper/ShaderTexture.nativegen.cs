namespace Ghost.Ufbx;

public unsafe struct ShaderTexture
{
    private ufbx_shader_texture* _ptr;

    internal ShaderTexture(ufbx_shader_texture* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ShaderTextureInput FindShaderTextureInputLen(sbyte* name, nuint nameLen)
    {
        return new(Api.ufbx_find_shader_texture_input_len(_ptr, name, nameLen));
    }

    public ShaderTextureInput FindShaderTextureInput(sbyte* name)
    {
        return new(Api.ufbx_find_shader_texture_input(_ptr, name));
    }

    public ufbx_shader_texture_type Type => _ptr->type;

    public ReadOnlySpan<byte> ShaderNameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->shader_name);
    public string ShaderName => NativeWrapperHelpers.GetString(_ptr->shader_name);

    public ulong ShaderTypeId => _ptr->shader_type_id;

    public ReadOnlySpan<ufbx_shader_texture_input> Inputs => _ptr->inputs.data == null ? ReadOnlySpan<ufbx_shader_texture_input>.Empty : new ReadOnlySpan<ufbx_shader_texture_input>(_ptr->inputs.data, checked((int)_ptr->inputs.count));

    public ReadOnlySpan<byte> ShaderSourceBytes => NativeWrapperHelpers.AsByteSpan(_ptr->shader_source);
    public string ShaderSource => NativeWrapperHelpers.GetString(_ptr->shader_source);

    public ReadOnlySpan<byte> RawShaderSource => NativeWrapperHelpers.AsSpan(_ptr->raw_shader_source);

    public bool HasMainTexture => _ptr->main_texture != null;
    public Texture MainTexture => _ptr->main_texture != null ? new(_ptr->main_texture) : throw new InvalidOperationException("MainTexture is null.");

    public long MainTextureOutputIndex => _ptr->main_texture_output_index;

    public ReadOnlySpan<byte> PropPrefixBytes => NativeWrapperHelpers.AsByteSpan(_ptr->prop_prefix);
    public string PropPrefix => NativeWrapperHelpers.GetString(_ptr->prop_prefix);

    internal ufbx_shader_texture* GetUnsafePtr() => _ptr;
}
