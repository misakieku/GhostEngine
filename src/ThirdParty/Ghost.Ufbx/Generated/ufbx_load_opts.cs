namespace Ghost.Ufbx
{
    /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts"]/*' />
    public partial struct ufbx_load_opts
    {
        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.temp_allocator"]/*' />
        public ufbx_allocator_opts temp_allocator;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.result_allocator"]/*' />
        public ufbx_allocator_opts result_allocator;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.thread_opts"]/*' />
        public ufbx_thread_opts thread_opts;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.ignore_geometry"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_geometry;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.ignore_animation"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_animation;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.ignore_embedded"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_embedded;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.ignore_all_content"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_all_content;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.evaluate_skinning"]/*' />
        [NativeTypeName("_Bool")]
        public bool evaluate_skinning;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.evaluate_caches"]/*' />
        [NativeTypeName("_Bool")]
        public bool evaluate_caches;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.load_external_files"]/*' />
        [NativeTypeName("_Bool")]
        public bool load_external_files;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.ignore_missing_external_files"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_missing_external_files;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.skip_skin_vertices"]/*' />
        [NativeTypeName("_Bool")]
        public bool skip_skin_vertices;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.skip_mesh_parts"]/*' />
        [NativeTypeName("_Bool")]
        public bool skip_mesh_parts;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.clean_skin_weights"]/*' />
        [NativeTypeName("_Bool")]
        public bool clean_skin_weights;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.use_blender_pbr_material"]/*' />
        [NativeTypeName("_Bool")]
        public bool use_blender_pbr_material;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.disable_quirks"]/*' />
        [NativeTypeName("_Bool")]
        public bool disable_quirks;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.strict"]/*' />
        [NativeTypeName("_Bool")]
        public bool strict;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.force_single_thread_ascii_parsing"]/*' />
        [NativeTypeName("_Bool")]
        public bool force_single_thread_ascii_parsing;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.allow_unsafe"]/*' />
        [NativeTypeName("_Bool")]
        public bool allow_unsafe;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.index_error_handling"]/*' />
        public ufbx_index_error_handling index_error_handling;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.connect_broken_elements"]/*' />
        [NativeTypeName("_Bool")]
        public bool connect_broken_elements;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.allow_nodes_out_of_root"]/*' />
        [NativeTypeName("_Bool")]
        public bool allow_nodes_out_of_root;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.allow_missing_vertex_position"]/*' />
        [NativeTypeName("_Bool")]
        public bool allow_missing_vertex_position;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.allow_empty_faces"]/*' />
        [NativeTypeName("_Bool")]
        public bool allow_empty_faces;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.generate_missing_normals"]/*' />
        [NativeTypeName("_Bool")]
        public bool generate_missing_normals;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.open_main_file_with_default"]/*' />
        [NativeTypeName("_Bool")]
        public bool open_main_file_with_default;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.path_separator"]/*' />
        [NativeTypeName("char")]
        public sbyte path_separator;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.node_depth_limit"]/*' />
        [NativeTypeName("uint32_t")]
        public uint node_depth_limit;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.file_size_estimate"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong file_size_estimate;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.read_buffer_size"]/*' />
        [NativeTypeName("size_t")]
        public nuint read_buffer_size;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.filename"]/*' />
        public ufbx_string filename;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.raw_filename"]/*' />
        public ufbx_blob raw_filename;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.progress_cb"]/*' />
        public ufbx_progress_cb progress_cb;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.progress_interval_hint"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong progress_interval_hint;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.open_file_cb"]/*' />
        public ufbx_open_file_cb open_file_cb;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.geometry_transform_handling"]/*' />
        public ufbx_geometry_transform_handling geometry_transform_handling;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.inherit_mode_handling"]/*' />
        public ufbx_inherit_mode_handling inherit_mode_handling;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.space_conversion"]/*' />
        public ufbx_space_conversion space_conversion;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.pivot_handling"]/*' />
        public ufbx_pivot_handling pivot_handling;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.pivot_handling_retain_empties"]/*' />
        [NativeTypeName("_Bool")]
        public bool pivot_handling_retain_empties;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.handedness_conversion_axis"]/*' />
        public ufbx_mirror_axis handedness_conversion_axis;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.handedness_conversion_retain_winding"]/*' />
        [NativeTypeName("_Bool")]
        public bool handedness_conversion_retain_winding;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.reverse_winding"]/*' />
        [NativeTypeName("_Bool")]
        public bool reverse_winding;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.target_axes"]/*' />
        public ufbx_coordinate_axes target_axes;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.target_unit_meters"]/*' />
        [NativeTypeName("ufbx_real")]
        public float target_unit_meters;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.target_camera_axes"]/*' />
        public ufbx_coordinate_axes target_camera_axes;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.target_light_axes"]/*' />
        public ufbx_coordinate_axes target_light_axes;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.geometry_transform_helper_name"]/*' />
        public ufbx_string geometry_transform_helper_name;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.scale_helper_name"]/*' />
        public ufbx_string scale_helper_name;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.normalize_normals"]/*' />
        [NativeTypeName("_Bool")]
        public bool normalize_normals;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.normalize_tangents"]/*' />
        [NativeTypeName("_Bool")]
        public bool normalize_tangents;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.use_root_transform"]/*' />
        [NativeTypeName("_Bool")]
        public bool use_root_transform;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.root_transform"]/*' />
        public ufbx_transform root_transform;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.key_clamp_threshold"]/*' />
        public double key_clamp_threshold;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.unicode_error_handling"]/*' />
        public ufbx_unicode_error_handling unicode_error_handling;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.retain_vertex_attrib_w"]/*' />
        [NativeTypeName("_Bool")]
        public bool retain_vertex_attrib_w;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.retain_dom"]/*' />
        [NativeTypeName("_Bool")]
        public bool retain_dom;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.file_format"]/*' />
        public ufbx_file_format file_format;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.file_format_lookahead"]/*' />
        [NativeTypeName("size_t")]
        public nuint file_format_lookahead;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.no_format_from_content"]/*' />
        [NativeTypeName("_Bool")]
        public bool no_format_from_content;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.no_format_from_extension"]/*' />
        [NativeTypeName("_Bool")]
        public bool no_format_from_extension;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.obj_search_mtl_by_filename"]/*' />
        [NativeTypeName("_Bool")]
        public bool obj_search_mtl_by_filename;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.obj_merge_objects"]/*' />
        [NativeTypeName("_Bool")]
        public bool obj_merge_objects;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.obj_merge_groups"]/*' />
        [NativeTypeName("_Bool")]
        public bool obj_merge_groups;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.obj_split_groups"]/*' />
        [NativeTypeName("_Bool")]
        public bool obj_split_groups;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.obj_mtl_path"]/*' />
        public ufbx_string obj_mtl_path;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.obj_mtl_data"]/*' />
        public ufbx_blob obj_mtl_data;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.obj_unit_meters"]/*' />
        [NativeTypeName("ufbx_real")]
        public float obj_unit_meters;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts.obj_axes"]/*' />
        public ufbx_coordinate_axes obj_axes;

        /// <include file='ufbx_load_opts.xml' path='doc/member[@name="ufbx_load_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
