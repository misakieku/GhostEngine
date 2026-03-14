namespace Ghost.Ufbx;

public unsafe struct MaterialTexture
{
    private ufbx_material_texture* _ptr;

    internal MaterialTexture(ufbx_material_texture* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> MaterialPropBytes => NativeWrapperHelpers.AsByteSpan(_ptr->material_prop);
    public string MaterialProp => NativeWrapperHelpers.GetString(_ptr->material_prop);

    public ReadOnlySpan<byte> ShaderPropBytes => NativeWrapperHelpers.AsByteSpan(_ptr->shader_prop);
    public string ShaderProp => NativeWrapperHelpers.GetString(_ptr->shader_prop);

    public bool HasTexture => _ptr->texture != null;
    public Texture Texture => _ptr->texture != null ? new(_ptr->texture) : throw new InvalidOperationException("Texture is null.");

    internal ufbx_material_texture* GetUnsafePtr() => _ptr;
}
