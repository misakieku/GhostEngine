using System.Runtime.InteropServices;
using static Ghost.Ufbx.ufbx_aperture_format;
using static Ghost.Ufbx.ufbx_aperture_mode;
using static Ghost.Ufbx.ufbx_aspect_mode;
using static Ghost.Ufbx.ufbx_bake_step_handling;
using static Ghost.Ufbx.ufbx_blend_mode;
using static Ghost.Ufbx.ufbx_cache_data_encoding;
using static Ghost.Ufbx.ufbx_cache_data_format;
using static Ghost.Ufbx.ufbx_cache_file_format;
using static Ghost.Ufbx.ufbx_cache_interpretation;
using static Ghost.Ufbx.ufbx_constraint_aim_up_type;
using static Ghost.Ufbx.ufbx_constraint_ik_pole_type;
using static Ghost.Ufbx.ufbx_constraint_type;
using static Ghost.Ufbx.ufbx_coordinate_axis;
using static Ghost.Ufbx.ufbx_dom_value_type;
using static Ghost.Ufbx.ufbx_element_type;
using static Ghost.Ufbx.ufbx_error_type;
using static Ghost.Ufbx.ufbx_exporter;
using static Ghost.Ufbx.ufbx_extrapolation_mode;
using static Ghost.Ufbx.ufbx_file_format;
using static Ghost.Ufbx.ufbx_gate_fit;
using static Ghost.Ufbx.ufbx_geometry_transform_handling;
using static Ghost.Ufbx.ufbx_index_error_handling;
using static Ghost.Ufbx.ufbx_inherit_mode;
using static Ghost.Ufbx.ufbx_inherit_mode_handling;
using static Ghost.Ufbx.ufbx_interpolation;
using static Ghost.Ufbx.ufbx_light_area_shape;
using static Ghost.Ufbx.ufbx_light_decay;
using static Ghost.Ufbx.ufbx_light_type;
using static Ghost.Ufbx.ufbx_lod_display;
using static Ghost.Ufbx.ufbx_marker_type;
using static Ghost.Ufbx.ufbx_material_fbx_map;
using static Ghost.Ufbx.ufbx_material_feature;
using static Ghost.Ufbx.ufbx_material_pbr_map;
using static Ghost.Ufbx.ufbx_mirror_axis;
using static Ghost.Ufbx.ufbx_nurbs_topology;
using static Ghost.Ufbx.ufbx_open_file_type;
using static Ghost.Ufbx.ufbx_pivot_handling;
using static Ghost.Ufbx.ufbx_projection_mode;
using static Ghost.Ufbx.ufbx_prop_type;
using static Ghost.Ufbx.ufbx_rotation_order;
using static Ghost.Ufbx.ufbx_shader_texture_type;
using static Ghost.Ufbx.ufbx_shader_type;
using static Ghost.Ufbx.ufbx_skinning_method;
using static Ghost.Ufbx.ufbx_snap_mode;
using static Ghost.Ufbx.ufbx_space_conversion;
using static Ghost.Ufbx.ufbx_subdivision_boundary;
using static Ghost.Ufbx.ufbx_subdivision_display_mode;
using static Ghost.Ufbx.ufbx_texture_type;
using static Ghost.Ufbx.ufbx_thumbnail_format;
using static Ghost.Ufbx.ufbx_time_mode;
using static Ghost.Ufbx.ufbx_time_protocol;
using static Ghost.Ufbx.ufbx_unicode_error_handling;
using static Ghost.Ufbx.ufbx_warning_type;
using static Ghost.Ufbx.ufbx_wrap_mode;

namespace Ghost.Ufbx
{
    public static unsafe partial class Api
    {
        public const int UFBX_ROTATION_ORDER_COUNT = (int)(UFBX_ROTATION_ORDER_SPHERIC + 1);

        public const int UFBX_DOM_VALUE_TYPE_COUNT = (int)(UFBX_DOM_VALUE_ARRAY_IGNORED + 1);

        public const int UFBX_PROP_TYPE_COUNT = (int)(UFBX_PROP_REFERENCE + 1);

        public const int UFBX_ELEMENT_TYPE_COUNT = (int)(UFBX_ELEMENT_METADATA_OBJECT + 1);

        public const int UFBX_INHERIT_MODE_COUNT = (int)(UFBX_INHERIT_MODE_COMPONENTWISE_SCALE + 1);

        public const int UFBX_MIRROR_AXIS_COUNT = (int)(UFBX_MIRROR_AXIS_Z + 1);

        public const int UFBX_SUBDIVISION_DISPLAY_MODE_COUNT = (int)(UFBX_SUBDIVISION_DISPLAY_SMOOTH + 1);

        public const int UFBX_SUBDIVISION_BOUNDARY_COUNT = (int)(UFBX_SUBDIVISION_BOUNDARY_SHARP_INTERIOR + 1);

        public const int UFBX_LIGHT_TYPE_COUNT = (int)(UFBX_LIGHT_VOLUME + 1);

        public const int UFBX_LIGHT_DECAY_COUNT = (int)(UFBX_LIGHT_DECAY_CUBIC + 1);

        public const int UFBX_LIGHT_AREA_SHAPE_COUNT = (int)(UFBX_LIGHT_AREA_SHAPE_SPHERE + 1);

        public const int UFBX_PROJECTION_MODE_COUNT = (int)(UFBX_PROJECTION_MODE_ORTHOGRAPHIC + 1);

        public const int UFBX_ASPECT_MODE_COUNT = (int)(UFBX_ASPECT_MODE_FIXED_HEIGHT + 1);

        public const int UFBX_APERTURE_MODE_COUNT = (int)(UFBX_APERTURE_MODE_FOCAL_LENGTH + 1);

        public const int UFBX_GATE_FIT_COUNT = (int)(UFBX_GATE_FIT_STRETCH + 1);

        public const int UFBX_APERTURE_FORMAT_COUNT = (int)(UFBX_APERTURE_FORMAT_IMAX + 1);

        public const int UFBX_COORDINATE_AXIS_COUNT = (int)(UFBX_COORDINATE_AXIS_UNKNOWN + 1);

        public const int UFBX_NURBS_TOPOLOGY_COUNT = (int)(UFBX_NURBS_TOPOLOGY_CLOSED + 1);

        public const int UFBX_MARKER_TYPE_COUNT = (int)(UFBX_MARKER_IK_EFFECTOR + 1);

        public const int UFBX_LOD_DISPLAY_COUNT = (int)(UFBX_LOD_DISPLAY_HIDE + 1);

        public const int UFBX_SKINNING_METHOD_COUNT = (int)(UFBX_SKINNING_METHOD_BLENDED_DQ_LINEAR + 1);

        public const int UFBX_CACHE_FILE_FORMAT_COUNT = (int)(UFBX_CACHE_FILE_FORMAT_MC + 1);

        public const int UFBX_CACHE_DATA_FORMAT_COUNT = (int)(UFBX_CACHE_DATA_FORMAT_VEC3_DOUBLE + 1);

        public const int UFBX_CACHE_DATA_ENCODING_COUNT = (int)(UFBX_CACHE_DATA_ENCODING_BIG_ENDIAN + 1);

        public const int UFBX_CACHE_INTERPRETATION_COUNT = (int)(UFBX_CACHE_INTERPRETATION_VERTEX_NORMAL + 1);

        public const int UFBX_SHADER_TYPE_COUNT = (int)(UFBX_SHADER_WAVEFRONT_MTL + 1);

        public const int UFBX_MATERIAL_FBX_MAP_COUNT = (int)(UFBX_MATERIAL_FBX_VECTOR_DISPLACEMENT + 1);

        public const int UFBX_MATERIAL_PBR_MAP_COUNT = (int)(UFBX_MATERIAL_PBR_TRANSMISSION_GLOSSINESS + 1);

        public const int UFBX_MATERIAL_FEATURE_COUNT = (int)(UFBX_MATERIAL_FEATURE_TRANSMISSION_ROUGHNESS_AS_GLOSSINESS + 1);

        public const int UFBX_TEXTURE_TYPE_COUNT = (int)(UFBX_TEXTURE_SHADER + 1);

        public const int UFBX_BLEND_MODE_COUNT = (int)(UFBX_BLEND_OVERLAY + 1);

        public const int UFBX_WRAP_MODE_COUNT = (int)(UFBX_WRAP_CLAMP + 1);

        public const int UFBX_SHADER_TEXTURE_TYPE_COUNT = (int)(UFBX_SHADER_TEXTURE_OSL + 1);

        public const int UFBX_INTERPOLATION_COUNT = (int)(UFBX_INTERPOLATION_CUBIC + 1);

        public const int UFBX_EXTRAPOLATION_MODE_COUNT = (int)(UFBX_EXTRAPOLATION_REPEAT_RELATIVE + 1);

        public const int UFBX_CONSTRAINT_TYPE_COUNT = (int)(UFBX_CONSTRAINT_SINGLE_CHAIN_IK + 1);

        public const int UFBX_CONSTRAINT_AIM_UP_TYPE_COUNT = (int)(UFBX_CONSTRAINT_AIM_UP_NONE + 1);

        public const int UFBX_CONSTRAINT_IK_POLE_TYPE_COUNT = (int)(UFBX_CONSTRAINT_IK_POLE_NODE + 1);

        public const int UFBX_EXPORTER_COUNT = (int)(UFBX_EXPORTER_MOTION_BUILDER + 1);

        public const int UFBX_FILE_FORMAT_COUNT = (int)(UFBX_FILE_FORMAT_MTL + 1);

        public const int UFBX_WARNING_TYPE_COUNT = (int)(UFBX_WARNING_UNKNOWN_OBJ_DIRECTIVE + 1);

        public const int UFBX_THUMBNAIL_FORMAT_COUNT = (int)(UFBX_THUMBNAIL_FORMAT_RGBA_32 + 1);

        public const int UFBX_SPACE_CONVERSION_COUNT = (int)(UFBX_SPACE_CONVERSION_MODIFY_GEOMETRY + 1);

        public const int UFBX_GEOMETRY_TRANSFORM_HANDLING_COUNT = (int)(UFBX_GEOMETRY_TRANSFORM_HANDLING_MODIFY_GEOMETRY_NO_FALLBACK + 1);

        public const int UFBX_INHERIT_MODE_HANDLING_COUNT = (int)(UFBX_INHERIT_MODE_HANDLING_IGNORE + 1);

        public const int UFBX_PIVOT_HANDLING_COUNT = (int)(UFBX_PIVOT_HANDLING_ADJUST_TO_ROTATION_PIVOT + 1);

        public const int UFBX_TIME_MODE_COUNT = (int)(UFBX_TIME_MODE_59_94_FPS + 1);

        public const int UFBX_TIME_PROTOCOL_COUNT = (int)(UFBX_TIME_PROTOCOL_DEFAULT + 1);

        public const int UFBX_SNAP_MODE_COUNT = (int)(UFBX_SNAP_MODE_SNAP_AND_PLAY + 1);

        public const int UFBX_OPEN_FILE_TYPE_COUNT = (int)(UFBX_OPEN_FILE_OBJ_MTL + 1);

        public const int UFBX_ERROR_TYPE_COUNT = (int)(UFBX_ERROR_UNSUPPORTED_VERSION + 1);

        public const int UFBX_INDEX_ERROR_HANDLING_COUNT = (int)(UFBX_INDEX_ERROR_HANDLING_UNSAFE_IGNORE + 1);

        public const int UFBX_UNICODE_ERROR_HANDLING_COUNT = (int)(UFBX_UNICODE_ERROR_HANDLING_UNSAFE_IGNORE + 1);

        public const int UFBX_BAKE_STEP_HANDLING_COUNT = (int)(UFBX_BAKE_STEP_HANDLING_IGNORE + 1);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_is_thread_safe();

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_scene* ufbx_load_memory([NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint data_size, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_scene* ufbx_load_file([NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_scene* ufbx_load_file_len([NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("size_t")] nuint filename_len, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_scene* ufbx_load_stdio(void* file, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_scene* ufbx_load_stdio_prefix(void* file, [NativeTypeName("const void *")] void* prefix, [NativeTypeName("size_t")] nuint prefix_size, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_scene* ufbx_load_stream([NativeTypeName("const ufbx_stream *")] ufbx_stream* stream, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_scene* ufbx_load_stream_prefix([NativeTypeName("const ufbx_stream *")] ufbx_stream* stream, [NativeTypeName("const void *")] void* prefix, [NativeTypeName("size_t")] nuint prefix_size, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_free_scene(ufbx_scene* scene);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_retain_scene(ufbx_scene* scene);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_format_error([NativeTypeName("char *")] sbyte* dst, [NativeTypeName("size_t")] nuint dst_size, [NativeTypeName("const ufbx_error *")] ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_prop* ufbx_find_prop_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_prop* ufbx_find_prop([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_find_real_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, [NativeTypeName("ufbx_real")] float def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_find_real([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("ufbx_real")] float def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_find_vec3_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, [NativeTypeName("ufbx_vec3")] Misaki.HighPerformance.Mathematics.float3 def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_find_vec3([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("ufbx_vec3")] Misaki.HighPerformance.Mathematics.float3 def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int64_t")]
        public static extern long ufbx_find_int_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, [NativeTypeName("int64_t")] long def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int64_t")]
        public static extern long ufbx_find_int([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("int64_t")] long def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_find_bool_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, [NativeTypeName("_Bool")] bool def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_find_bool([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("_Bool")] bool def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_string ufbx_find_string_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, ufbx_string def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_string ufbx_find_string([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, ufbx_string def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_blob ufbx_find_blob_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, ufbx_blob def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_blob ufbx_find_blob([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, ufbx_blob def);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_prop* ufbx_find_prop_concat([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const ufbx_string *")] ufbx_string* parts, [NativeTypeName("size_t")] nuint num_parts);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_element* ufbx_get_prop_element([NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const ufbx_prop *")] ufbx_prop* prop, ufbx_element_type type);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_element* ufbx_find_prop_element_len([NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, ufbx_element_type type);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_element* ufbx_find_prop_element([NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, ufbx_element_type type);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_element* ufbx_find_element_len([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, ufbx_element_type type, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_element* ufbx_find_element([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, ufbx_element_type type, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_node* ufbx_find_node_len([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_node* ufbx_find_node([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_stack* ufbx_find_anim_stack_len([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_stack* ufbx_find_anim_stack([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_material* ufbx_find_material_len([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_material* ufbx_find_material([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_prop* ufbx_find_anim_prop_len([NativeTypeName("const ufbx_anim_layer *")] ufbx_anim_layer* layer, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* prop, [NativeTypeName("size_t")] nuint prop_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_prop* ufbx_find_anim_prop([NativeTypeName("const ufbx_anim_layer *")] ufbx_anim_layer* layer, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* prop);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_prop_list ufbx_find_anim_props([NativeTypeName("const ufbx_anim_layer *")] ufbx_anim_layer* layer, [NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_matrix")]
        public static extern Misaki.HighPerformance.Mathematics.float3x4 ufbx_get_compatible_matrix_for_normals([NativeTypeName("const ufbx_node *")] ufbx_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ptrdiff_t")]
        public static extern nint ufbx_inflate(void* dst, [NativeTypeName("size_t")] nuint dst_size, [NativeTypeName("const ufbx_inflate_input *")] ufbx_inflate_input* input, ufbx_inflate_retain* retain);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_default_open_file(void* user, ufbx_stream* stream, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("size_t")] nuint path_len, [NativeTypeName("const ufbx_open_file_info *")] ufbx_open_file_info* info);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_open_file(ufbx_stream* stream, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("size_t")] nuint path_len, [NativeTypeName("const ufbx_open_file_opts *")] ufbx_open_file_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_open_file_ctx(ufbx_stream* stream, [NativeTypeName("ufbx_open_file_context")] nuint ctx, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("size_t")] nuint path_len, [NativeTypeName("const ufbx_open_file_opts *")] ufbx_open_file_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_open_memory(ufbx_stream* stream, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint data_size, [NativeTypeName("const ufbx_open_memory_opts *")] ufbx_open_memory_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_open_memory_ctx(ufbx_stream* stream, [NativeTypeName("ufbx_open_file_context")] nuint ctx, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint data_size, [NativeTypeName("const ufbx_open_memory_opts *")] ufbx_open_memory_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_evaluate_curve([NativeTypeName("const ufbx_anim_curve *")] ufbx_anim_curve* curve, double time, [NativeTypeName("ufbx_real")] float default_value);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_evaluate_curve_flags([NativeTypeName("const ufbx_anim_curve *")] ufbx_anim_curve* curve, double time, [NativeTypeName("ufbx_real")] float default_value, [NativeTypeName("uint32_t")] uint flags);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_evaluate_anim_value_real([NativeTypeName("const ufbx_anim_value *")] ufbx_anim_value* anim_value, double time);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_evaluate_anim_value_vec3([NativeTypeName("const ufbx_anim_value *")] ufbx_anim_value* anim_value, double time);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_evaluate_anim_value_real_flags([NativeTypeName("const ufbx_anim_value *")] ufbx_anim_value* anim_value, double time, [NativeTypeName("uint32_t")] uint flags);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_evaluate_anim_value_vec3_flags([NativeTypeName("const ufbx_anim_value *")] ufbx_anim_value* anim_value, double time, [NativeTypeName("uint32_t")] uint flags);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_prop ufbx_evaluate_prop_len([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, double time);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_prop ufbx_evaluate_prop([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, double time);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_prop ufbx_evaluate_prop_flags_len([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, double time, [NativeTypeName("uint32_t")] uint flags);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_prop ufbx_evaluate_prop_flags([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, double time, [NativeTypeName("uint32_t")] uint flags);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_props ufbx_evaluate_props([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, double time, ufbx_prop* buffer, [NativeTypeName("size_t")] nuint buffer_size);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_props ufbx_evaluate_props_flags([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, double time, ufbx_prop* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("uint32_t")] uint flags);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_transform ufbx_evaluate_transform([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_node *")] ufbx_node* node, double time);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_transform ufbx_evaluate_transform_flags([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_node *")] ufbx_node* node, double time, [NativeTypeName("uint32_t")] uint flags);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_evaluate_blend_weight([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_blend_channel *")] ufbx_blend_channel* channel, double time);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_evaluate_blend_weight_flags([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_blend_channel *")] ufbx_blend_channel* channel, double time, [NativeTypeName("uint32_t")] uint flags);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_scene* ufbx_evaluate_scene([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, double time, [NativeTypeName("const ufbx_evaluate_opts *")] ufbx_evaluate_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim* ufbx_create_anim([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const ufbx_anim_opts *")] ufbx_anim_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_free_anim(ufbx_anim* anim);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_retain_anim(ufbx_anim* anim);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_baked_anim* ufbx_bake_anim([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_bake_opts *")] ufbx_bake_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_retain_baked_anim(ufbx_baked_anim* bake);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_free_baked_anim(ufbx_baked_anim* bake);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_baked_node* ufbx_find_baked_node_by_typed_id(ufbx_baked_anim* bake, [NativeTypeName("uint32_t")] uint typed_id);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_baked_node* ufbx_find_baked_node(ufbx_baked_anim* bake, ufbx_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_baked_element* ufbx_find_baked_element_by_element_id(ufbx_baked_anim* bake, [NativeTypeName("uint32_t")] uint element_id);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_baked_element* ufbx_find_baked_element(ufbx_baked_anim* bake, ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_evaluate_baked_vec3(ufbx_baked_vec3_list keyframes, double time);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_quat ufbx_evaluate_baked_quat(ufbx_baked_quat_list keyframes, double time);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_bone_pose* ufbx_get_bone_pose([NativeTypeName("const ufbx_pose *")] ufbx_pose* pose, [NativeTypeName("const ufbx_node *")] ufbx_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_texture* ufbx_find_prop_texture_len([NativeTypeName("const ufbx_material *")] ufbx_material* material, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_texture* ufbx_find_prop_texture([NativeTypeName("const ufbx_material *")] ufbx_material* material, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_string ufbx_find_shader_prop_len([NativeTypeName("const ufbx_shader *")] ufbx_shader* shader, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_string ufbx_find_shader_prop([NativeTypeName("const ufbx_shader *")] ufbx_shader* shader, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_shader_prop_binding_list ufbx_find_shader_prop_bindings_len([NativeTypeName("const ufbx_shader *")] ufbx_shader* shader, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_shader_prop_binding_list ufbx_find_shader_prop_bindings([NativeTypeName("const ufbx_shader *")] ufbx_shader* shader, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_shader_texture_input* ufbx_find_shader_texture_input_len([NativeTypeName("const ufbx_shader_texture *")] ufbx_shader_texture* shader, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_shader_texture_input* ufbx_find_shader_texture_input([NativeTypeName("const ufbx_shader_texture *")] ufbx_shader_texture* shader, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_coordinate_axes_valid(ufbx_coordinate_axes axes);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_vec3_normalize([NativeTypeName("ufbx_vec3")] Misaki.HighPerformance.Mathematics.float3 v);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_quat_dot(ufbx_quat a, ufbx_quat b);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_quat ufbx_quat_mul(ufbx_quat a, ufbx_quat b);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_quat ufbx_quat_normalize(ufbx_quat q);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_quat ufbx_quat_fix_antipodal(ufbx_quat q, ufbx_quat reference);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_quat ufbx_quat_slerp(ufbx_quat a, ufbx_quat b, [NativeTypeName("ufbx_real")] float t);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_quat_rotate_vec3(ufbx_quat q, [NativeTypeName("ufbx_vec3")] Misaki.HighPerformance.Mathematics.float3 v);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_quat_to_euler(ufbx_quat q, ufbx_rotation_order order);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_quat ufbx_euler_to_quat([NativeTypeName("ufbx_vec3")] Misaki.HighPerformance.Mathematics.float3 v, ufbx_rotation_order order);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_matrix")]
        public static extern Misaki.HighPerformance.Mathematics.float3x4 ufbx_matrix_mul([NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* a, [NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* b);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_matrix_determinant([NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* m);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_matrix")]
        public static extern Misaki.HighPerformance.Mathematics.float3x4 ufbx_matrix_invert([NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* m);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_matrix")]
        public static extern Misaki.HighPerformance.Mathematics.float3x4 ufbx_matrix_for_normals([NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* m);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_transform_position([NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* m, [NativeTypeName("ufbx_vec3")] Misaki.HighPerformance.Mathematics.float3 v);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_transform_direction([NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* m, [NativeTypeName("ufbx_vec3")] Misaki.HighPerformance.Mathematics.float3 v);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_matrix")]
        public static extern Misaki.HighPerformance.Mathematics.float3x4 ufbx_transform_to_matrix([NativeTypeName("const ufbx_transform *")] ufbx_transform* t);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_transform ufbx_matrix_to_transform([NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* m);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_matrix")]
        public static extern Misaki.HighPerformance.Mathematics.float3x4 ufbx_catch_get_skin_vertex_matrix(ufbx_panic* panic, [NativeTypeName("const ufbx_skin_deformer *")] ufbx_skin_deformer* skin, [NativeTypeName("size_t")] nuint vertex, [NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* fallback);

        [return: NativeTypeName("ufbx_matrix")]
        public static Misaki.HighPerformance.Mathematics.float3x4 ufbx_get_skin_vertex_matrix([NativeTypeName("const ufbx_skin_deformer *")] ufbx_skin_deformer* skin, [NativeTypeName("size_t")] nuint vertex, [NativeTypeName("const ufbx_matrix *")] Misaki.HighPerformance.Mathematics.float3x4* fallback)
        {
            return ufbx_catch_get_skin_vertex_matrix(null, skin, vertex, fallback);
        }

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ufbx_get_blend_shape_offset_index([NativeTypeName("const ufbx_blend_shape *")] ufbx_blend_shape* shape, [NativeTypeName("size_t")] nuint vertex);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_get_blend_shape_vertex_offset([NativeTypeName("const ufbx_blend_shape *")] ufbx_blend_shape* shape, [NativeTypeName("size_t")] nuint vertex);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_get_blend_vertex_offset([NativeTypeName("const ufbx_blend_deformer *")] ufbx_blend_deformer* blend, [NativeTypeName("size_t")] nuint vertex);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_add_blend_shape_vertex_offsets([NativeTypeName("const ufbx_blend_shape *")] ufbx_blend_shape* shape, [NativeTypeName("ufbx_vec3 *")] Misaki.HighPerformance.Mathematics.float3* vertices, [NativeTypeName("size_t")] nuint num_vertices, [NativeTypeName("ufbx_real")] float weight);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_add_blend_vertex_offsets([NativeTypeName("const ufbx_blend_deformer *")] ufbx_blend_deformer* blend, [NativeTypeName("ufbx_vec3 *")] Misaki.HighPerformance.Mathematics.float3* vertices, [NativeTypeName("size_t")] nuint num_vertices, [NativeTypeName("ufbx_real")] float weight);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_evaluate_nurbs_basis([NativeTypeName("const ufbx_nurbs_basis *")] ufbx_nurbs_basis* basis, [NativeTypeName("ufbx_real")] float u, [NativeTypeName("ufbx_real *")] float* weights, [NativeTypeName("size_t")] nuint num_weights, [NativeTypeName("ufbx_real *")] float* derivatives, [NativeTypeName("size_t")] nuint num_derivatives);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_curve_point ufbx_evaluate_nurbs_curve([NativeTypeName("const ufbx_nurbs_curve *")] ufbx_nurbs_curve* curve, [NativeTypeName("ufbx_real")] float u);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_surface_point ufbx_evaluate_nurbs_surface([NativeTypeName("const ufbx_nurbs_surface *")] ufbx_nurbs_surface* surface, [NativeTypeName("ufbx_real")] float u, [NativeTypeName("ufbx_real")] float v);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_line_curve* ufbx_tessellate_nurbs_curve([NativeTypeName("const ufbx_nurbs_curve *")] ufbx_nurbs_curve* curve, [NativeTypeName("const ufbx_tessellate_curve_opts *")] ufbx_tessellate_curve_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_mesh* ufbx_tessellate_nurbs_surface([NativeTypeName("const ufbx_nurbs_surface *")] ufbx_nurbs_surface* surface, [NativeTypeName("const ufbx_tessellate_surface_opts *")] ufbx_tessellate_surface_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_free_line_curve(ufbx_line_curve* curve);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_retain_line_curve(ufbx_line_curve* curve);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ufbx_find_face_index(ufbx_mesh* mesh, [NativeTypeName("size_t")] nuint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ufbx_catch_triangulate_face(ufbx_panic* panic, [NativeTypeName("uint32_t *")] uint* indices, [NativeTypeName("size_t")] nuint num_indices, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, ufbx_face face);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ufbx_triangulate_face([NativeTypeName("uint32_t *")] uint* indices, [NativeTypeName("size_t")] nuint num_indices, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, ufbx_face face);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_catch_compute_topology(ufbx_panic* panic, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_compute_topology([NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ufbx_catch_topo_next_vertex_edge(ufbx_panic* panic, [NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t")] uint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ufbx_topo_next_vertex_edge([NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t")] uint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ufbx_catch_topo_prev_vertex_edge(ufbx_panic* panic, [NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t")] uint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint ufbx_topo_prev_vertex_edge([NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t")] uint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_catch_get_weighted_face_normal(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* positions, ufbx_face face);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_get_weighted_face_normal([NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* positions, ufbx_face face);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_catch_generate_normal_mapping(ufbx_panic* panic, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t *")] uint* normal_indices, [NativeTypeName("size_t")] nuint num_normal_indices, [NativeTypeName("_Bool")] bool assume_smooth);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_generate_normal_mapping([NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t *")] uint* normal_indices, [NativeTypeName("size_t")] nuint num_normal_indices, [NativeTypeName("_Bool")] bool assume_smooth);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_catch_compute_normals(ufbx_panic* panic, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* positions, [NativeTypeName("const uint32_t *")] uint* normal_indices, [NativeTypeName("size_t")] nuint num_normal_indices, [NativeTypeName("ufbx_vec3 *")] Misaki.HighPerformance.Mathematics.float3* normals, [NativeTypeName("size_t")] nuint num_normals);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_compute_normals([NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* positions, [NativeTypeName("const uint32_t *")] uint* normal_indices, [NativeTypeName("size_t")] nuint num_normal_indices, [NativeTypeName("ufbx_vec3 *")] Misaki.HighPerformance.Mathematics.float3* normals, [NativeTypeName("size_t")] nuint num_normals);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_mesh* ufbx_subdivide_mesh([NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("size_t")] nuint level, [NativeTypeName("const ufbx_subdivide_opts *")] ufbx_subdivide_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_free_mesh(ufbx_mesh* mesh);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_retain_mesh(ufbx_mesh* mesh);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_geometry_cache* ufbx_load_geometry_cache([NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("const ufbx_geometry_cache_opts *")] ufbx_geometry_cache_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_geometry_cache* ufbx_load_geometry_cache_len([NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("size_t")] nuint filename_len, [NativeTypeName("const ufbx_geometry_cache_opts *")] ufbx_geometry_cache_opts* opts, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_free_geometry_cache(ufbx_geometry_cache* cache);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_retain_geometry_cache(ufbx_geometry_cache* cache);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_read_geometry_cache_real([NativeTypeName("const ufbx_cache_frame *")] ufbx_cache_frame* frame, [NativeTypeName("ufbx_real *")] float* data, [NativeTypeName("size_t")] nuint num_data, [NativeTypeName("const ufbx_geometry_cache_data_opts *")] ufbx_geometry_cache_data_opts* opts);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_read_geometry_cache_vec3([NativeTypeName("const ufbx_cache_frame *")] ufbx_cache_frame* frame, [NativeTypeName("ufbx_vec3 *")] Misaki.HighPerformance.Mathematics.float3* data, [NativeTypeName("size_t")] nuint num_data, [NativeTypeName("const ufbx_geometry_cache_data_opts *")] ufbx_geometry_cache_data_opts* opts);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_sample_geometry_cache_real([NativeTypeName("const ufbx_cache_channel *")] ufbx_cache_channel* channel, double time, [NativeTypeName("ufbx_real *")] float* data, [NativeTypeName("size_t")] nuint num_data, [NativeTypeName("const ufbx_geometry_cache_data_opts *")] ufbx_geometry_cache_data_opts* opts);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_sample_geometry_cache_vec3([NativeTypeName("const ufbx_cache_channel *")] ufbx_cache_channel* channel, double time, [NativeTypeName("ufbx_vec3 *")] Misaki.HighPerformance.Mathematics.float3* data, [NativeTypeName("size_t")] nuint num_data, [NativeTypeName("const ufbx_geometry_cache_data_opts *")] ufbx_geometry_cache_data_opts* opts);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_dom_node* ufbx_dom_find_len([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* parent, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_dom_node* ufbx_dom_find([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* parent, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_generate_indices([NativeTypeName("const ufbx_vertex_stream *")] ufbx_vertex_stream* streams, [NativeTypeName("size_t")] nuint num_streams, [NativeTypeName("uint32_t *")] uint* indices, [NativeTypeName("size_t")] nuint num_indices, [NativeTypeName("const ufbx_allocator_opts *")] ufbx_allocator_opts* allocator, ufbx_error* error);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_thread_pool_run_task([NativeTypeName("ufbx_thread_pool_context")] nuint ctx, [NativeTypeName("uint32_t")] uint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ufbx_thread_pool_set_user_ptr([NativeTypeName("ufbx_thread_pool_context")] nuint ctx, void* user_ptr);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* ufbx_thread_pool_get_user_ptr([NativeTypeName("ufbx_thread_pool_context")] nuint ctx);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_catch_get_vertex_real(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_real *")] ufbx_vertex_real* v, [NativeTypeName("size_t")] nuint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec2")]
        public static extern Misaki.HighPerformance.Mathematics.float2 ufbx_catch_get_vertex_vec2(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec2 *")] ufbx_vertex_vec2* v, [NativeTypeName("size_t")] nuint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec3")]
        public static extern Misaki.HighPerformance.Mathematics.float3 ufbx_catch_get_vertex_vec3(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* v, [NativeTypeName("size_t")] nuint index);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_vec4")]
        public static extern Misaki.HighPerformance.Mathematics.float4 ufbx_catch_get_vertex_vec4(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec4 *")] ufbx_vertex_vec4* v, [NativeTypeName("size_t")] nuint index);

        [return: NativeTypeName("ufbx_real")]
        public static float ufbx_get_vertex_real([NativeTypeName("const ufbx_vertex_real *")] ufbx_vertex_real* v, [NativeTypeName("size_t")] nuint index)
        {
            ;
            return v->values.data[(int)(v->indices.data[index])];
        }

        [return: NativeTypeName("ufbx_vec2")]
        public static Misaki.HighPerformance.Mathematics.float2 ufbx_get_vertex_vec2([NativeTypeName("const ufbx_vertex_vec2 *")] ufbx_vertex_vec2* v, [NativeTypeName("size_t")] nuint index)
        {
            ;
            return v->values.data[(int)(v->indices.data[index])];
        }

        [return: NativeTypeName("ufbx_vec3")]
        public static Misaki.HighPerformance.Mathematics.float3 ufbx_get_vertex_vec3([NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* v, [NativeTypeName("size_t")] nuint index)
        {
            ;
            return v->values.data[(int)(v->indices.data[index])];
        }

        [return: NativeTypeName("ufbx_vec4")]
        public static Misaki.HighPerformance.Mathematics.float4 ufbx_get_vertex_vec4([NativeTypeName("const ufbx_vertex_vec4 *")] ufbx_vertex_vec4* v, [NativeTypeName("size_t")] nuint index)
        {
            ;
            return v->values.data[(int)(v->indices.data[index])];
        }

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("ufbx_real")]
        public static extern float ufbx_catch_get_vertex_w_vec3(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* v, [NativeTypeName("size_t")] nuint index);

        [return: NativeTypeName("ufbx_real")]
        public static float ufbx_get_vertex_w_vec3([NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* v, [NativeTypeName("size_t")] nuint index)
        {
            ;
            return v->values_w.count > 0 ? v->values_w.data[(int)(v->indices.data[index])] : 0.0f;
        }

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_unknown* ufbx_as_unknown([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_node* ufbx_as_node([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_mesh* ufbx_as_mesh([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_light* ufbx_as_light([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_camera* ufbx_as_camera([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_bone* ufbx_as_bone([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_empty* ufbx_as_empty([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_line_curve* ufbx_as_line_curve([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_nurbs_curve* ufbx_as_nurbs_curve([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_nurbs_surface* ufbx_as_nurbs_surface([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_nurbs_trim_surface* ufbx_as_nurbs_trim_surface([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_nurbs_trim_boundary* ufbx_as_nurbs_trim_boundary([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_procedural_geometry* ufbx_as_procedural_geometry([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_stereo_camera* ufbx_as_stereo_camera([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_camera_switcher* ufbx_as_camera_switcher([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_marker* ufbx_as_marker([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_lod_group* ufbx_as_lod_group([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_skin_deformer* ufbx_as_skin_deformer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_skin_cluster* ufbx_as_skin_cluster([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_blend_deformer* ufbx_as_blend_deformer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_blend_channel* ufbx_as_blend_channel([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_blend_shape* ufbx_as_blend_shape([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_cache_deformer* ufbx_as_cache_deformer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_cache_file* ufbx_as_cache_file([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_material* ufbx_as_material([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_texture* ufbx_as_texture([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_video* ufbx_as_video([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_shader* ufbx_as_shader([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_shader_binding* ufbx_as_shader_binding([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_stack* ufbx_as_anim_stack([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_layer* ufbx_as_anim_layer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_value* ufbx_as_anim_value([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_anim_curve* ufbx_as_anim_curve([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_display_layer* ufbx_as_display_layer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_selection_set* ufbx_as_selection_set([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_selection_node* ufbx_as_selection_node([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_character* ufbx_as_character([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_constraint* ufbx_as_constraint([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_audio_layer* ufbx_as_audio_layer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_audio_clip* ufbx_as_audio_clip([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_pose* ufbx_as_pose([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_metadata_object* ufbx_as_metadata_object([NativeTypeName("const ufbx_element *")] ufbx_element* element);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern bool ufbx_dom_is_array([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint ufbx_dom_array_size([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_int32_list ufbx_dom_as_int32_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_int64_list ufbx_dom_as_int64_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_float_list ufbx_dom_as_float_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_double_list ufbx_dom_as_double_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_real_list ufbx_dom_as_real_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

        [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ufbx_blob_list ufbx_dom_as_blob_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

        [NativeTypeName("#define UFBX_STDC __STDC_VERSION__")]
        public const int UFBX_STDC = 201710;

        [NativeTypeName("#define UFBX_CPP 0")]
        public const int UFBX_CPP = 0;

        [NativeTypeName("#define UFBX_PLATFORM_MSC _MSC_VER")]
        public const int UFBX_PLATFORM_MSC = 1950;

        [NativeTypeName("#define UFBX_PLATFORM_GNUC 0")]
        public const int UFBX_PLATFORM_GNUC = 0;

        [NativeTypeName("#define UFBX_CPP11 0")]
        public const int UFBX_CPP11 = 0;

        [NativeTypeName("#define UFBX_ERROR_STACK_MAX_DEPTH 8")]
        public const int UFBX_ERROR_STACK_MAX_DEPTH = 8;

        [NativeTypeName("#define UFBX_PANIC_MESSAGE_LENGTH 128")]
        public const int UFBX_PANIC_MESSAGE_LENGTH = 128;

        [NativeTypeName("#define UFBX_ERROR_INFO_LENGTH 256")]
        public const int UFBX_ERROR_INFO_LENGTH = 256;

        [NativeTypeName("#define UFBX_THREAD_GROUP_COUNT 4")]
        public const int UFBX_THREAD_GROUP_COUNT = 4;

        [NativeTypeName("#define UFBX_HAS_FORCE_32BIT 1")]
        public const int UFBX_HAS_FORCE_32BIT = 1;

        [NativeTypeName("#define UFBX_HEADER_VERSION ufbx_pack_version(0, 21, 3)")]
        public const uint UFBX_HEADER_VERSION = ((0) * 1000000U + (21) * 1000U + (3));

        [NativeTypeName("#define UFBX_VERSION UFBX_HEADER_VERSION")]
        public const uint UFBX_VERSION = ((0) * 1000000U + (21) * 1000U + (3));

        [NativeTypeName("#define UFBX_NO_INDEX ((uint32_t)~0u)")]
        public const uint UFBX_NO_INDEX = ((uint)(~0U));

        [NativeTypeName("#define UFBX_Lcl_Translation \"Lcl Translation\"")]
        public static ReadOnlySpan<byte> UFBX_Lcl_Translation => "Lcl Translation"u8;

        [NativeTypeName("#define UFBX_Lcl_Rotation \"Lcl Rotation\"")]
        public static ReadOnlySpan<byte> UFBX_Lcl_Rotation => "Lcl Rotation"u8;

        [NativeTypeName("#define UFBX_Lcl_Scaling \"Lcl Scaling\"")]
        public static ReadOnlySpan<byte> UFBX_Lcl_Scaling => "Lcl Scaling"u8;

        [NativeTypeName("#define UFBX_RotationOrder \"RotationOrder\"")]
        public static ReadOnlySpan<byte> UFBX_RotationOrder => "RotationOrder"u8;

        [NativeTypeName("#define UFBX_ScalingPivot \"ScalingPivot\"")]
        public static ReadOnlySpan<byte> UFBX_ScalingPivot => "ScalingPivot"u8;

        [NativeTypeName("#define UFBX_RotationPivot \"RotationPivot\"")]
        public static ReadOnlySpan<byte> UFBX_RotationPivot => "RotationPivot"u8;

        [NativeTypeName("#define UFBX_ScalingOffset \"ScalingOffset\"")]
        public static ReadOnlySpan<byte> UFBX_ScalingOffset => "ScalingOffset"u8;

        [NativeTypeName("#define UFBX_RotationOffset \"RotationOffset\"")]
        public static ReadOnlySpan<byte> UFBX_RotationOffset => "RotationOffset"u8;

        [NativeTypeName("#define UFBX_PreRotation \"PreRotation\"")]
        public static ReadOnlySpan<byte> UFBX_PreRotation => "PreRotation"u8;

        [NativeTypeName("#define UFBX_PostRotation \"PostRotation\"")]
        public static ReadOnlySpan<byte> UFBX_PostRotation => "PostRotation"u8;

        [NativeTypeName("#define UFBX_Visibility \"Visibility\"")]
        public static ReadOnlySpan<byte> UFBX_Visibility => "Visibility"u8;

        [NativeTypeName("#define UFBX_Weight \"Weight\"")]
        public static ReadOnlySpan<byte> UFBX_Weight => "Weight"u8;

        [NativeTypeName("#define UFBX_DeformPercent \"DeformPercent\"")]
        public static ReadOnlySpan<byte> UFBX_DeformPercent => "DeformPercent"u8;
    }
}
