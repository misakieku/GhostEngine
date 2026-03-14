namespace Ghost.Ufbx;

public unsafe struct Metadata
{
    private ufbx_metadata* _ptr;

    internal Metadata(ufbx_metadata* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<ufbx_warning> Warnings => _ptr->warnings.data == null ? ReadOnlySpan<ufbx_warning>.Empty : new ReadOnlySpan<ufbx_warning>(_ptr->warnings.data, checked((int)_ptr->warnings.count));

    public bool Ascii => _ptr->ascii;

    public uint Version => _ptr->version;

    public ufbx_file_format FileFormat => _ptr->file_format;

    public bool MayContainNoIndex => _ptr->may_contain_no_index;

    public bool MayContainMissingVertexPosition => _ptr->may_contain_missing_vertex_position;

    public bool MayContainBrokenElements => _ptr->may_contain_broken_elements;

    public bool IsUnsafe => _ptr->is_unsafe;

    public ReadOnlySpan<byte> CreatorBytes => NativeWrapperHelpers.AsByteSpan(_ptr->creator);
    public string Creator => NativeWrapperHelpers.GetString(_ptr->creator);

    public bool BigEndian => _ptr->big_endian;

    public ReadOnlySpan<byte> FilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->filename);
    public string Filename => NativeWrapperHelpers.GetString(_ptr->filename);

    public ReadOnlySpan<byte> RelativeRootBytes => NativeWrapperHelpers.AsByteSpan(_ptr->relative_root);
    public string RelativeRoot => NativeWrapperHelpers.GetString(_ptr->relative_root);

    public ReadOnlySpan<byte> RawFilename => NativeWrapperHelpers.AsSpan(_ptr->raw_filename);

    public ReadOnlySpan<byte> RawRelativeRoot => NativeWrapperHelpers.AsSpan(_ptr->raw_relative_root);

    public ufbx_exporter Exporter => _ptr->exporter;

    public uint ExporterVersion => _ptr->exporter_version;

    public Props SceneProps => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->scene_props));

    public Application OriginalApplication => new((ufbx_application*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->original_application));

    public Application LatestApplication => new((ufbx_application*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->latest_application));

    public Thumbnail Thumbnail => new((ufbx_thumbnail*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->thumbnail));

    public bool GeometryIgnored => _ptr->geometry_ignored;

    public bool AnimationIgnored => _ptr->animation_ignored;

    public bool EmbeddedIgnored => _ptr->embedded_ignored;

    public nuint MaxFaceTriangles => _ptr->max_face_triangles;

    public nuint ResultMemoryUsed => _ptr->result_memory_used;

    public nuint TempMemoryUsed => _ptr->temp_memory_used;

    public nuint ResultAllocs => _ptr->result_allocs;

    public nuint TempAllocs => _ptr->temp_allocs;

    public nuint ElementBufferSize => _ptr->element_buffer_size;

    public nuint NumShaderTextures => _ptr->num_shader_textures;

    public float BonePropSizeUnit => _ptr->bone_prop_size_unit;

    public bool BonePropLimbLengthRelative => _ptr->bone_prop_limb_length_relative;

    public float OrthoSizeUnit => _ptr->ortho_size_unit;

    public long KtimeSecond => _ptr->ktime_second;

    public ReadOnlySpan<byte> OriginalFilePathBytes => NativeWrapperHelpers.AsByteSpan(_ptr->original_file_path);
    public string OriginalFilePath => NativeWrapperHelpers.GetString(_ptr->original_file_path);

    public ReadOnlySpan<byte> RawOriginalFilePath => NativeWrapperHelpers.AsSpan(_ptr->raw_original_file_path);

    public ufbx_space_conversion SpaceConversion => _ptr->space_conversion;

    public ufbx_geometry_transform_handling GeometryTransformHandling => _ptr->geometry_transform_handling;

    public ufbx_inherit_mode_handling InheritModeHandling => _ptr->inherit_mode_handling;

    public ufbx_pivot_handling PivotHandling => _ptr->pivot_handling;

    public ufbx_mirror_axis HandednessConversionAxis => _ptr->handedness_conversion_axis;

    public Misaki.HighPerformance.Mathematics.quaternion RootRotation => _ptr->root_rotation;

    public float RootScale => _ptr->root_scale;

    public ufbx_mirror_axis MirrorAxis => _ptr->mirror_axis;

    public float GeometryScale => _ptr->geometry_scale;

    internal ufbx_metadata* GetUnsafePtr() => _ptr;
}
