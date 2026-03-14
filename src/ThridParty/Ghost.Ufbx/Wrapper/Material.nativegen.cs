namespace Ghost.Ufbx;

public unsafe ref struct Material
{
    private ufbx_material* _ptr;

    internal Material(ufbx_material* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Texture FindPropTextureLen(sbyte* name, nuint nameLen)
    {
        return new(Api.ufbx_find_prop_texture_len(_ptr, name, nameLen));
    }

    public Texture FindPropTexture(sbyte* name)
    {
        return new(Api.ufbx_find_prop_texture(_ptr, name));
    }

    public MaterialFbxMaps Fbx => new((ufbx_material_fbx_maps*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->fbx));

    public MaterialPbrMaps Pbr => new((ufbx_material_pbr_maps*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->pbr));

    public MaterialFeatures Features => new((ufbx_material_features*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->features));

    public ufbx_shader_type ShaderType => _ptr->shader_type;

    public bool HasShader => _ptr->shader != null;
    public Shader Shader => _ptr->shader != null ? new(_ptr->shader) : throw new InvalidOperationException("Shader is null.");

    public ReadOnlySpan<byte> ShadingModelNameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->shading_model_name);
    public string ShadingModelName => NativeWrapperHelpers.GetString(_ptr->shading_model_name);

    public ReadOnlySpan<byte> ShaderPropPrefixBytes => NativeWrapperHelpers.AsByteSpan(_ptr->shader_prop_prefix);
    public string ShaderPropPrefix => NativeWrapperHelpers.GetString(_ptr->shader_prop_prefix);

    public ReadOnlySpan<ufbx_material_texture> Textures => _ptr->textures.data == null ? ReadOnlySpan<ufbx_material_texture>.Empty : new ReadOnlySpan<ufbx_material_texture>(_ptr->textures.data, checked((int)_ptr->textures.count));

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_material* GetUnsafePtr() => _ptr;
}
