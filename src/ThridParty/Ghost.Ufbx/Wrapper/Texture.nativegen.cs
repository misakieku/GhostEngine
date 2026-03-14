namespace Ghost.Ufbx;

public unsafe ref struct Texture
{
    private ufbx_texture* _ptr;

    internal Texture(ufbx_texture* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_texture_type Type => _ptr->type;

    public ReadOnlySpan<byte> FilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->filename);
    public string Filename => NativeWrapperHelpers.GetString(_ptr->filename);

    public ReadOnlySpan<byte> AbsoluteFilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->absolute_filename);
    public string AbsoluteFilename => NativeWrapperHelpers.GetString(_ptr->absolute_filename);

    public ReadOnlySpan<byte> RelativeFilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->relative_filename);
    public string RelativeFilename => NativeWrapperHelpers.GetString(_ptr->relative_filename);

    public ReadOnlySpan<byte> RawFilename => NativeWrapperHelpers.AsSpan(_ptr->raw_filename);

    public ReadOnlySpan<byte> RawAbsoluteFilename => NativeWrapperHelpers.AsSpan(_ptr->raw_absolute_filename);

    public ReadOnlySpan<byte> RawRelativeFilename => NativeWrapperHelpers.AsSpan(_ptr->raw_relative_filename);

    public ReadOnlySpan<byte> Content => NativeWrapperHelpers.AsSpan(_ptr->content);

    public bool HasVideo => _ptr->video != null;
    public Video Video => _ptr->video != null ? new(_ptr->video) : throw new InvalidOperationException("Video is null.");

    public uint FileIndex => _ptr->file_index;

    public bool HasFile => _ptr->has_file;

    public ReadOnlySpan<ufbx_texture_layer> Layers => _ptr->layers.data == null ? ReadOnlySpan<ufbx_texture_layer>.Empty : new ReadOnlySpan<ufbx_texture_layer>(_ptr->layers.data, checked((int)_ptr->layers.count));

    public bool HasShader => _ptr->shader != null;
    public ShaderTexture Shader => _ptr->shader != null ? new(_ptr->shader) : throw new InvalidOperationException("Shader is null.");

    public TextureList FileTextures => new(_ptr->file_textures.data, _ptr->file_textures.count);

    public ReadOnlySpan<byte> UvSetBytes => NativeWrapperHelpers.AsByteSpan(_ptr->uv_set);
    public string UvSet => NativeWrapperHelpers.GetString(_ptr->uv_set);

    public ufbx_wrap_mode WrapU => _ptr->wrap_u;

    public ufbx_wrap_mode WrapV => _ptr->wrap_v;

    public bool HasUvTransform => _ptr->has_uv_transform;

    public Transform UvTransform => new((ufbx_transform*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->uv_transform));

    public Misaki.HighPerformance.Mathematics.float3x4 TextureToUv => _ptr->texture_to_uv;

    public Misaki.HighPerformance.Mathematics.float3x4 UvToTexture => _ptr->uv_to_texture;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_texture* GetUnsafePtr() => _ptr;
}
