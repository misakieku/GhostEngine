namespace Ghost.Ufbx;

public unsafe struct ShaderTextureInput
{
    private ufbx_shader_texture_input* _ptr;

    internal ShaderTextureInput(ufbx_shader_texture_input* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public long ValueInt => _ptr->value_int;

    public ReadOnlySpan<byte> ValueStrBytes => NativeWrapperHelpers.AsByteSpan(_ptr->value_str);
    public string ValueStr => NativeWrapperHelpers.GetString(_ptr->value_str);

    public ReadOnlySpan<byte> ValueBlob => NativeWrapperHelpers.AsSpan(_ptr->value_blob);

    public bool HasTexture => _ptr->texture != null;
    public Texture Texture => _ptr->texture != null ? new(_ptr->texture) : throw new InvalidOperationException("Texture is null.");

    public long TextureOutputIndex => _ptr->texture_output_index;

    public bool TextureEnabled => _ptr->texture_enabled;

    public bool HasProp => _ptr->prop != null;
    public Prop Prop => _ptr->prop != null ? new(_ptr->prop) : throw new InvalidOperationException("Prop is null.");

    public bool HasTextureProp => _ptr->texture_prop != null;
    public Prop TextureProp => _ptr->texture_prop != null ? new(_ptr->texture_prop) : throw new InvalidOperationException("TextureProp is null.");

    public bool HasTextureEnabledProp => _ptr->texture_enabled_prop != null;
    public Prop TextureEnabledProp => _ptr->texture_enabled_prop != null ? new(_ptr->texture_enabled_prop) : throw new InvalidOperationException("TextureEnabledProp is null.");

    public float ValueReal => _ptr->value_real;

    public Misaki.HighPerformance.Mathematics.float2 ValueVec2 => _ptr->value_vec2;

    public Misaki.HighPerformance.Mathematics.float3 ValueVec3 => _ptr->value_vec3;

    public Misaki.HighPerformance.Mathematics.float4 ValueVec4 => _ptr->value_vec4;

    internal ufbx_shader_texture_input* GetUnsafePtr() => _ptr;
}
