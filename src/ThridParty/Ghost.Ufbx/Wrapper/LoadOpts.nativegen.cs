namespace Ghost.Ufbx;

public unsafe partial class LoadOpts : System.IDisposable
{
    private ufbx_load_opts* _ptr;
    private bool _csAlloc;

    public LoadOpts()
    {
        _ptr = (ufbx_load_opts*)System.Runtime.InteropServices.NativeMemory.AllocZeroed((nuint)sizeof(ufbx_load_opts));
        _csAlloc = true;
    }

    internal LoadOpts(ufbx_load_opts* ptr)
    {
        _ptr = ptr;
        _csAlloc = false;
    }

    public bool IsNull => _ptr == null;

    public partial void Dispose();

    public ufbx_allocator_opts TempAllocator { get => _ptr->temp_allocator; set => _ptr->temp_allocator = value; }

    public ufbx_allocator_opts ResultAllocator { get => _ptr->result_allocator; set => _ptr->result_allocator = value; }

    public ufbx_thread_opts ThreadOpts { get => _ptr->thread_opts; set => _ptr->thread_opts = value; }

    public bool IgnoreGeometry { get => _ptr->ignore_geometry; set => _ptr->ignore_geometry = value; }

    public bool IgnoreAnimation { get => _ptr->ignore_animation; set => _ptr->ignore_animation = value; }

    public bool IgnoreEmbedded { get => _ptr->ignore_embedded; set => _ptr->ignore_embedded = value; }

    public bool IgnoreAllContent { get => _ptr->ignore_all_content; set => _ptr->ignore_all_content = value; }

    public bool EvaluateSkinning { get => _ptr->evaluate_skinning; set => _ptr->evaluate_skinning = value; }

    public bool EvaluateCaches { get => _ptr->evaluate_caches; set => _ptr->evaluate_caches = value; }

    public bool LoadExternalFiles { get => _ptr->load_external_files; set => _ptr->load_external_files = value; }

    public bool IgnoreMissingExternalFiles { get => _ptr->ignore_missing_external_files; set => _ptr->ignore_missing_external_files = value; }

    public bool SkipSkinVertices { get => _ptr->skip_skin_vertices; set => _ptr->skip_skin_vertices = value; }

    public bool SkipMeshParts { get => _ptr->skip_mesh_parts; set => _ptr->skip_mesh_parts = value; }

    public bool CleanSkinWeights { get => _ptr->clean_skin_weights; set => _ptr->clean_skin_weights = value; }

    public bool UseBlenderPbrMaterial { get => _ptr->use_blender_pbr_material; set => _ptr->use_blender_pbr_material = value; }

    public bool DisableQuirks { get => _ptr->disable_quirks; set => _ptr->disable_quirks = value; }

    public bool Strict { get => _ptr->strict; set => _ptr->strict = value; }

    public bool ForceSingleThreadAsciiParsing { get => _ptr->force_single_thread_ascii_parsing; set => _ptr->force_single_thread_ascii_parsing = value; }

    public bool AllowUnsafe { get => _ptr->allow_unsafe; set => _ptr->allow_unsafe = value; }

    public ufbx_index_error_handling IndexErrorHandling { get => _ptr->index_error_handling; set => _ptr->index_error_handling = value; }

    public bool ConnectBrokenElements { get => _ptr->connect_broken_elements; set => _ptr->connect_broken_elements = value; }

    public bool AllowNodesOutOfRoot { get => _ptr->allow_nodes_out_of_root; set => _ptr->allow_nodes_out_of_root = value; }

    public bool AllowMissingVertexPosition { get => _ptr->allow_missing_vertex_position; set => _ptr->allow_missing_vertex_position = value; }

    public bool AllowEmptyFaces { get => _ptr->allow_empty_faces; set => _ptr->allow_empty_faces = value; }

    public bool GenerateMissingNormals { get => _ptr->generate_missing_normals; set => _ptr->generate_missing_normals = value; }

    public bool OpenMainFileWithDefault { get => _ptr->open_main_file_with_default; set => _ptr->open_main_file_with_default = value; }

    public sbyte PathSeparator { get => _ptr->path_separator; set => _ptr->path_separator = value; }

    public uint NodeDepthLimit { get => _ptr->node_depth_limit; set => _ptr->node_depth_limit = value; }

    public ulong FileSizeEstimate { get => _ptr->file_size_estimate; set => _ptr->file_size_estimate = value; }

    public nuint ReadBufferSize { get => _ptr->read_buffer_size; set => _ptr->read_buffer_size = value; }

    private cstring _filename;
    public partial cstring Filename { get; set; }

    public ReadOnlySpan<byte> RawFilename => NativeWrapperHelpers.AsSpan(_ptr->raw_filename);

    public ufbx_progress_cb ProgressCb { get => _ptr->progress_cb; set => _ptr->progress_cb = value; }

    public ulong ProgressIntervalHint { get => _ptr->progress_interval_hint; set => _ptr->progress_interval_hint = value; }

    public ufbx_open_file_cb OpenFileCb { get => _ptr->open_file_cb; set => _ptr->open_file_cb = value; }

    public ufbx_geometry_transform_handling GeometryTransformHandling { get => _ptr->geometry_transform_handling; set => _ptr->geometry_transform_handling = value; }

    public ufbx_inherit_mode_handling InheritModeHandling { get => _ptr->inherit_mode_handling; set => _ptr->inherit_mode_handling = value; }

    public ufbx_space_conversion SpaceConversion { get => _ptr->space_conversion; set => _ptr->space_conversion = value; }

    public ufbx_pivot_handling PivotHandling { get => _ptr->pivot_handling; set => _ptr->pivot_handling = value; }

    public bool PivotHandlingRetainEmpties { get => _ptr->pivot_handling_retain_empties; set => _ptr->pivot_handling_retain_empties = value; }

    public ufbx_mirror_axis HandednessConversionAxis { get => _ptr->handedness_conversion_axis; set => _ptr->handedness_conversion_axis = value; }

    public bool HandednessConversionRetainWinding { get => _ptr->handedness_conversion_retain_winding; set => _ptr->handedness_conversion_retain_winding = value; }

    public bool ReverseWinding { get => _ptr->reverse_winding; set => _ptr->reverse_winding = value; }

    public ufbx_coordinate_axes TargetAxes { get => _ptr->target_axes; set => _ptr->target_axes = value; }

    public float TargetUnitMeters { get => _ptr->target_unit_meters; set => _ptr->target_unit_meters = value; }

    public ufbx_coordinate_axes TargetCameraAxes { get => _ptr->target_camera_axes; set => _ptr->target_camera_axes = value; }

    public ufbx_coordinate_axes TargetLightAxes { get => _ptr->target_light_axes; set => _ptr->target_light_axes = value; }

    public ReadOnlySpan<byte> GeometryTransformHelperNameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->geometry_transform_helper_name);
    public string GeometryTransformHelperName => NativeWrapperHelpers.GetString(_ptr->geometry_transform_helper_name);

    public ReadOnlySpan<byte> ScaleHelperNameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->scale_helper_name);
    public string ScaleHelperName => NativeWrapperHelpers.GetString(_ptr->scale_helper_name);

    public bool NormalizeNormals { get => _ptr->normalize_normals; set => _ptr->normalize_normals = value; }

    public bool NormalizeTangents { get => _ptr->normalize_tangents; set => _ptr->normalize_tangents = value; }

    public bool UseRootTransform { get => _ptr->use_root_transform; set => _ptr->use_root_transform = value; }

    public ufbx_transform RootTransform { get => _ptr->root_transform; set => _ptr->root_transform = value; }

    public double KeyClampThreshold { get => _ptr->key_clamp_threshold; set => _ptr->key_clamp_threshold = value; }

    public ufbx_unicode_error_handling UnicodeErrorHandling { get => _ptr->unicode_error_handling; set => _ptr->unicode_error_handling = value; }

    public bool RetainVertexAttribW { get => _ptr->retain_vertex_attrib_w; set => _ptr->retain_vertex_attrib_w = value; }

    public bool RetainDom { get => _ptr->retain_dom; set => _ptr->retain_dom = value; }

    public ufbx_file_format FileFormat { get => _ptr->file_format; set => _ptr->file_format = value; }

    public nuint FileFormatLookahead { get => _ptr->file_format_lookahead; set => _ptr->file_format_lookahead = value; }

    public bool NoFormatFromContent { get => _ptr->no_format_from_content; set => _ptr->no_format_from_content = value; }

    public bool NoFormatFromExtension { get => _ptr->no_format_from_extension; set => _ptr->no_format_from_extension = value; }

    public bool ObjSearchMtlByFilename { get => _ptr->obj_search_mtl_by_filename; set => _ptr->obj_search_mtl_by_filename = value; }

    public bool ObjMergeObjects { get => _ptr->obj_merge_objects; set => _ptr->obj_merge_objects = value; }

    public bool ObjMergeGroups { get => _ptr->obj_merge_groups; set => _ptr->obj_merge_groups = value; }

    public bool ObjSplitGroups { get => _ptr->obj_split_groups; set => _ptr->obj_split_groups = value; }

    private cstring _objMtlPath;
    public partial cstring ObjMtlPath { get; set; }

    public ReadOnlySpan<byte> ObjMtlData => NativeWrapperHelpers.AsSpan(_ptr->obj_mtl_data);

    public float ObjUnitMeters { get => _ptr->obj_unit_meters; set => _ptr->obj_unit_meters = value; }

    public ufbx_coordinate_axes ObjAxes { get => _ptr->obj_axes; set => _ptr->obj_axes = value; }

    internal ufbx_load_opts* GetUnsafePtr() => _ptr;
}
