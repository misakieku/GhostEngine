namespace Ghost.Ufbx
{
    public partial struct ufbx_load_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_allocator_opts temp_allocator;

        public ufbx_allocator_opts result_allocator;

        public ufbx_thread_opts thread_opts;

        [NativeTypeName("_Bool")]
        public bool ignore_geometry;

        [NativeTypeName("_Bool")]
        public bool ignore_animation;

        [NativeTypeName("_Bool")]
        public bool ignore_embedded;

        [NativeTypeName("_Bool")]
        public bool ignore_all_content;

        [NativeTypeName("_Bool")]
        public bool evaluate_skinning;

        [NativeTypeName("_Bool")]
        public bool evaluate_caches;

        [NativeTypeName("_Bool")]
        public bool load_external_files;

        [NativeTypeName("_Bool")]
        public bool ignore_missing_external_files;

        [NativeTypeName("_Bool")]
        public bool skip_skin_vertices;

        [NativeTypeName("_Bool")]
        public bool skip_mesh_parts;

        [NativeTypeName("_Bool")]
        public bool clean_skin_weights;

        [NativeTypeName("_Bool")]
        public bool use_blender_pbr_material;

        [NativeTypeName("_Bool")]
        public bool disable_quirks;

        [NativeTypeName("_Bool")]
        public bool strict;

        [NativeTypeName("_Bool")]
        public bool force_single_thread_ascii_parsing;

        [NativeTypeName("_Bool")]
        public bool allow_unsafe;

        public ufbx_index_error_handling index_error_handling;

        [NativeTypeName("_Bool")]
        public bool connect_broken_elements;

        [NativeTypeName("_Bool")]
        public bool allow_nodes_out_of_root;

        [NativeTypeName("_Bool")]
        public bool allow_missing_vertex_position;

        [NativeTypeName("_Bool")]
        public bool allow_empty_faces;

        [NativeTypeName("_Bool")]
        public bool generate_missing_normals;

        [NativeTypeName("_Bool")]
        public bool open_main_file_with_default;

        [NativeTypeName("char")]
        public sbyte path_separator;

        [NativeTypeName("uint32_t")]
        public uint node_depth_limit;

        [NativeTypeName("uint64_t")]
        public ulong file_size_estimate;

        [NativeTypeName("size_t")]
        public nuint read_buffer_size;

        public ufbx_string filename;

        public ufbx_blob raw_filename;

        public ufbx_progress_cb progress_cb;

        [NativeTypeName("uint64_t")]
        public ulong progress_interval_hint;

        public ufbx_open_file_cb open_file_cb;

        public ufbx_geometry_transform_handling geometry_transform_handling;

        public ufbx_inherit_mode_handling inherit_mode_handling;

        public ufbx_space_conversion space_conversion;

        public ufbx_pivot_handling pivot_handling;

        [NativeTypeName("_Bool")]
        public bool pivot_handling_retain_empties;

        public ufbx_mirror_axis handedness_conversion_axis;

        [NativeTypeName("_Bool")]
        public bool handedness_conversion_retain_winding;

        [NativeTypeName("_Bool")]
        public bool reverse_winding;

        public ufbx_coordinate_axes target_axes;

        [NativeTypeName("ufbx_real")]
        public float target_unit_meters;

        public ufbx_coordinate_axes target_camera_axes;

        public ufbx_coordinate_axes target_light_axes;

        public ufbx_string geometry_transform_helper_name;

        public ufbx_string scale_helper_name;

        [NativeTypeName("_Bool")]
        public bool normalize_normals;

        [NativeTypeName("_Bool")]
        public bool normalize_tangents;

        [NativeTypeName("_Bool")]
        public bool use_root_transform;

        public ufbx_transform root_transform;

        public double key_clamp_threshold;

        public ufbx_unicode_error_handling unicode_error_handling;

        [NativeTypeName("_Bool")]
        public bool retain_vertex_attrib_w;

        [NativeTypeName("_Bool")]
        public bool retain_dom;

        public ufbx_file_format file_format;

        [NativeTypeName("size_t")]
        public nuint file_format_lookahead;

        [NativeTypeName("_Bool")]
        public bool no_format_from_content;

        [NativeTypeName("_Bool")]
        public bool no_format_from_extension;

        [NativeTypeName("_Bool")]
        public bool obj_search_mtl_by_filename;

        [NativeTypeName("_Bool")]
        public bool obj_merge_objects;

        [NativeTypeName("_Bool")]
        public bool obj_merge_groups;

        [NativeTypeName("_Bool")]
        public bool obj_split_groups;

        public ufbx_string obj_mtl_path;

        public ufbx_blob obj_mtl_data;

        [NativeTypeName("ufbx_real")]
        public float obj_unit_meters;

        public ufbx_coordinate_axes obj_axes;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
