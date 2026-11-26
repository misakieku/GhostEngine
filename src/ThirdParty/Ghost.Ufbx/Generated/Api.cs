using System.Runtime.InteropServices;

namespace Ghost.Ufbx;

public static unsafe partial class Api
{
    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_ROTATION_ORDER_COUNT"]/*' />
    public const int UFBX_ROTATION_ORDER_COUNT = 7;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_DOM_VALUE_TYPE_COUNT"]/*' />
    public const int UFBX_DOM_VALUE_TYPE_COUNT = 9;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_PROP_TYPE_COUNT"]/*' />
    public const int UFBX_PROP_TYPE_COUNT = 16;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_ELEMENT_TYPE_COUNT"]/*' />
    public const int UFBX_ELEMENT_TYPE_COUNT = 42;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_INHERIT_MODE_COUNT"]/*' />
    public const int UFBX_INHERIT_MODE_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_MIRROR_AXIS_COUNT"]/*' />
    public const int UFBX_MIRROR_AXIS_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_SUBDIVISION_DISPLAY_MODE_COUNT"]/*' />
    public const int UFBX_SUBDIVISION_DISPLAY_MODE_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_SUBDIVISION_BOUNDARY_COUNT"]/*' />
    public const int UFBX_SUBDIVISION_BOUNDARY_COUNT = 6;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_LIGHT_TYPE_COUNT"]/*' />
    public const int UFBX_LIGHT_TYPE_COUNT = 5;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_LIGHT_DECAY_COUNT"]/*' />
    public const int UFBX_LIGHT_DECAY_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_LIGHT_AREA_SHAPE_COUNT"]/*' />
    public const int UFBX_LIGHT_AREA_SHAPE_COUNT = 2;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_PROJECTION_MODE_COUNT"]/*' />
    public const int UFBX_PROJECTION_MODE_COUNT = 2;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_ASPECT_MODE_COUNT"]/*' />
    public const int UFBX_ASPECT_MODE_COUNT = 5;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_APERTURE_MODE_COUNT"]/*' />
    public const int UFBX_APERTURE_MODE_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_GATE_FIT_COUNT"]/*' />
    public const int UFBX_GATE_FIT_COUNT = 6;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_APERTURE_FORMAT_COUNT"]/*' />
    public const int UFBX_APERTURE_FORMAT_COUNT = 12;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_COORDINATE_AXIS_COUNT"]/*' />
    public const int UFBX_COORDINATE_AXIS_COUNT = 7;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_NURBS_TOPOLOGY_COUNT"]/*' />
    public const int UFBX_NURBS_TOPOLOGY_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_MARKER_TYPE_COUNT"]/*' />
    public const int UFBX_MARKER_TYPE_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_LOD_DISPLAY_COUNT"]/*' />
    public const int UFBX_LOD_DISPLAY_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_SKINNING_METHOD_COUNT"]/*' />
    public const int UFBX_SKINNING_METHOD_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CACHE_FILE_FORMAT_COUNT"]/*' />
    public const int UFBX_CACHE_FILE_FORMAT_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CACHE_DATA_FORMAT_COUNT"]/*' />
    public const int UFBX_CACHE_DATA_FORMAT_COUNT = 5;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CACHE_DATA_ENCODING_COUNT"]/*' />
    public const int UFBX_CACHE_DATA_ENCODING_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CACHE_INTERPRETATION_COUNT"]/*' />
    public const int UFBX_CACHE_INTERPRETATION_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_SHADER_TYPE_COUNT"]/*' />
    public const int UFBX_SHADER_TYPE_COUNT = 13;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_MATERIAL_FBX_MAP_COUNT"]/*' />
    public const int UFBX_MATERIAL_FBX_MAP_COUNT = 20;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_MATERIAL_PBR_MAP_COUNT"]/*' />
    public const int UFBX_MATERIAL_PBR_MAP_COUNT = 56;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_MATERIAL_FEATURE_COUNT"]/*' />
    public const int UFBX_MATERIAL_FEATURE_COUNT = 23;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_TEXTURE_TYPE_COUNT"]/*' />
    public const int UFBX_TEXTURE_TYPE_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_BLEND_MODE_COUNT"]/*' />
    public const int UFBX_BLEND_MODE_COUNT = 31;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_WRAP_MODE_COUNT"]/*' />
    public const int UFBX_WRAP_MODE_COUNT = 2;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_SHADER_TEXTURE_TYPE_COUNT"]/*' />
    public const int UFBX_SHADER_TEXTURE_TYPE_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_INTERPOLATION_COUNT"]/*' />
    public const int UFBX_INTERPOLATION_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_EXTRAPOLATION_MODE_COUNT"]/*' />
    public const int UFBX_EXTRAPOLATION_MODE_COUNT = 5;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CONSTRAINT_TYPE_COUNT"]/*' />
    public const int UFBX_CONSTRAINT_TYPE_COUNT = 7;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CONSTRAINT_AIM_UP_TYPE_COUNT"]/*' />
    public const int UFBX_CONSTRAINT_AIM_UP_TYPE_COUNT = 5;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CONSTRAINT_IK_POLE_TYPE_COUNT"]/*' />
    public const int UFBX_CONSTRAINT_IK_POLE_TYPE_COUNT = 2;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_EXPORTER_COUNT"]/*' />
    public const int UFBX_EXPORTER_COUNT = 6;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_FILE_FORMAT_COUNT"]/*' />
    public const int UFBX_FILE_FORMAT_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_WARNING_TYPE_COUNT"]/*' />
    public const int UFBX_WARNING_TYPE_COUNT = 15;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_THUMBNAIL_FORMAT_COUNT"]/*' />
    public const int UFBX_THUMBNAIL_FORMAT_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_SPACE_CONVERSION_COUNT"]/*' />
    public const int UFBX_SPACE_CONVERSION_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_GEOMETRY_TRANSFORM_HANDLING_COUNT"]/*' />
    public const int UFBX_GEOMETRY_TRANSFORM_HANDLING_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_INHERIT_MODE_HANDLING_COUNT"]/*' />
    public const int UFBX_INHERIT_MODE_HANDLING_COUNT = 5;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_PIVOT_HANDLING_COUNT"]/*' />
    public const int UFBX_PIVOT_HANDLING_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_TIME_MODE_COUNT"]/*' />
    public const int UFBX_TIME_MODE_COUNT = 18;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_TIME_PROTOCOL_COUNT"]/*' />
    public const int UFBX_TIME_PROTOCOL_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_SNAP_MODE_COUNT"]/*' />
    public const int UFBX_SNAP_MODE_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_OPEN_FILE_TYPE_COUNT"]/*' />
    public const int UFBX_OPEN_FILE_TYPE_COUNT = 3;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_ERROR_TYPE_COUNT"]/*' />
    public const int UFBX_ERROR_TYPE_COUNT = 24;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_INDEX_ERROR_HANDLING_COUNT"]/*' />
    public const int UFBX_INDEX_ERROR_HANDLING_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_UNICODE_ERROR_HANDLING_COUNT"]/*' />
    public const int UFBX_UNICODE_ERROR_HANDLING_COUNT = 6;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_BAKE_STEP_HANDLING_COUNT"]/*' />
    public const int UFBX_BAKE_STEP_HANDLING_COUNT = 5;

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_is_thread_safe"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_is_thread_safe();

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_memory"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_scene* ufbx_load_memory([NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint data_size, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_file"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_scene* ufbx_load_file([NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_file_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_scene* ufbx_load_file_len([NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("size_t")] nuint filename_len, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_stdio"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_scene* ufbx_load_stdio(void* file, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_stdio_prefix"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_scene* ufbx_load_stdio_prefix(void* file, [NativeTypeName("const void *")] void* prefix, [NativeTypeName("size_t")] nuint prefix_size, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_stream"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_scene* ufbx_load_stream([NativeTypeName("const ufbx_stream *")] ufbx_stream* stream, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_stream_prefix"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_scene* ufbx_load_stream_prefix([NativeTypeName("const ufbx_stream *")] ufbx_stream* stream, [NativeTypeName("const void *")] void* prefix, [NativeTypeName("size_t")] nuint prefix_size, [NativeTypeName("const ufbx_load_opts *")] ufbx_load_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_free_scene"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_free_scene(ufbx_scene* scene);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_retain_scene"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_retain_scene(ufbx_scene* scene);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_format_error"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_format_error([NativeTypeName("char *")] sbyte* dst, [NativeTypeName("size_t")] nuint dst_size, [NativeTypeName("const ufbx_error *")] ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_prop_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_prop* ufbx_find_prop_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_prop"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_prop* ufbx_find_prop([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_real_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_find_real_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, [NativeTypeName("ufbx_real")] float def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_real"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_find_real([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("ufbx_real")] float def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_vec3_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_find_vec3_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, ufbx_vec3 def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_vec3"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_find_vec3([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, ufbx_vec3 def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_int_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("int64_t")]
    public static extern long ufbx_find_int_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, [NativeTypeName("int64_t")] long def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_int"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("int64_t")]
    public static extern long ufbx_find_int([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("int64_t")] long def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_bool_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_find_bool_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, [NativeTypeName("_Bool")] bool def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_bool"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_find_bool([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("_Bool")] bool def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_string_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_string ufbx_find_string_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, ufbx_string def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_string"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_string ufbx_find_string([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, ufbx_string def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_blob_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_blob ufbx_find_blob_len([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, ufbx_blob def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_blob"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_blob ufbx_find_blob([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const char *")] sbyte* name, ufbx_blob def);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_prop_concat"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_prop* ufbx_find_prop_concat([NativeTypeName("const ufbx_props *")] ufbx_props* props, [NativeTypeName("const ufbx_string *")] ufbx_string* parts, [NativeTypeName("size_t")] nuint num_parts);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_get_prop_element"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_element* ufbx_get_prop_element([NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const ufbx_prop *")] ufbx_prop* prop, ufbx_element_type type);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_prop_element_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_element* ufbx_find_prop_element_len([NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, ufbx_element_type type);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_prop_element"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_element* ufbx_find_prop_element([NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, ufbx_element_type type);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_element_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_element* ufbx_find_element_len([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, ufbx_element_type type, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_element"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_element* ufbx_find_element([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, ufbx_element_type type, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_node_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_node* ufbx_find_node_len([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_node"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_node* ufbx_find_node([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_anim_stack_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_stack* ufbx_find_anim_stack_len([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_anim_stack"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_stack* ufbx_find_anim_stack([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_material_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_material* ufbx_find_material_len([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_material"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_material* ufbx_find_material([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_anim_prop_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_prop* ufbx_find_anim_prop_len([NativeTypeName("const ufbx_anim_layer *")] ufbx_anim_layer* layer, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* prop, [NativeTypeName("size_t")] nuint prop_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_anim_prop"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_prop* ufbx_find_anim_prop([NativeTypeName("const ufbx_anim_layer *")] ufbx_anim_layer* layer, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* prop);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_anim_props"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_prop_list ufbx_find_anim_props([NativeTypeName("const ufbx_anim_layer *")] ufbx_anim_layer* layer, [NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_get_compatible_matrix_for_normals"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_matrix ufbx_get_compatible_matrix_for_normals([NativeTypeName("const ufbx_node *")] ufbx_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_inflate"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ptrdiff_t")]
    public static extern nint ufbx_inflate(void* dst, [NativeTypeName("size_t")] nuint dst_size, [NativeTypeName("const ufbx_inflate_input *")] ufbx_inflate_input* input, ufbx_inflate_retain* retain);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_default_open_file"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_default_open_file(void* user, ufbx_stream* stream, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("size_t")] nuint path_len, [NativeTypeName("const ufbx_open_file_info *")] ufbx_open_file_info* info);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_open_file"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_open_file(ufbx_stream* stream, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("size_t")] nuint path_len, [NativeTypeName("const ufbx_open_file_opts *")] ufbx_open_file_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_open_file_ctx"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_open_file_ctx(ufbx_stream* stream, [NativeTypeName("ufbx_open_file_context")] nuint ctx, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("size_t")] nuint path_len, [NativeTypeName("const ufbx_open_file_opts *")] ufbx_open_file_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_open_memory"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_open_memory(ufbx_stream* stream, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint data_size, [NativeTypeName("const ufbx_open_memory_opts *")] ufbx_open_memory_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_open_memory_ctx"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_open_memory_ctx(ufbx_stream* stream, [NativeTypeName("ufbx_open_file_context")] nuint ctx, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint data_size, [NativeTypeName("const ufbx_open_memory_opts *")] ufbx_open_memory_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_curve"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_evaluate_curve([NativeTypeName("const ufbx_anim_curve *")] ufbx_anim_curve* curve, double time, [NativeTypeName("ufbx_real")] float default_value);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_curve_flags"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_evaluate_curve_flags([NativeTypeName("const ufbx_anim_curve *")] ufbx_anim_curve* curve, double time, [NativeTypeName("ufbx_real")] float default_value, [NativeTypeName("uint32_t")] uint flags);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_anim_value_real"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_evaluate_anim_value_real([NativeTypeName("const ufbx_anim_value *")] ufbx_anim_value* anim_value, double time);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_anim_value_vec3"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_evaluate_anim_value_vec3([NativeTypeName("const ufbx_anim_value *")] ufbx_anim_value* anim_value, double time);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_anim_value_real_flags"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_evaluate_anim_value_real_flags([NativeTypeName("const ufbx_anim_value *")] ufbx_anim_value* anim_value, double time, [NativeTypeName("uint32_t")] uint flags);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_anim_value_vec3_flags"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_evaluate_anim_value_vec3_flags([NativeTypeName("const ufbx_anim_value *")] ufbx_anim_value* anim_value, double time, [NativeTypeName("uint32_t")] uint flags);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_prop_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_prop ufbx_evaluate_prop_len([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, double time);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_prop"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_prop ufbx_evaluate_prop([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, double time);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_prop_flags_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_prop ufbx_evaluate_prop_flags_len([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len, double time, [NativeTypeName("uint32_t")] uint flags);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_prop_flags"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_prop ufbx_evaluate_prop_flags([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, [NativeTypeName("const char *")] sbyte* name, double time, [NativeTypeName("uint32_t")] uint flags);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_props"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_props ufbx_evaluate_props([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, double time, ufbx_prop* buffer, [NativeTypeName("size_t")] nuint buffer_size);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_props_flags"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_props ufbx_evaluate_props_flags([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_element *")] ufbx_element* element, double time, ufbx_prop* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("uint32_t")] uint flags);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_transform"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_transform ufbx_evaluate_transform([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_node *")] ufbx_node* node, double time);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_transform_flags"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_transform ufbx_evaluate_transform_flags([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_node *")] ufbx_node* node, double time, [NativeTypeName("uint32_t")] uint flags);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_blend_weight"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_evaluate_blend_weight([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_blend_channel *")] ufbx_blend_channel* channel, double time);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_blend_weight_flags"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_evaluate_blend_weight_flags([NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_blend_channel *")] ufbx_blend_channel* channel, double time, [NativeTypeName("uint32_t")] uint flags);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_scene"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_scene* ufbx_evaluate_scene([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, double time, [NativeTypeName("const ufbx_evaluate_opts *")] ufbx_evaluate_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_create_anim"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim* ufbx_create_anim([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const ufbx_anim_opts *")] ufbx_anim_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_free_anim"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_free_anim(ufbx_anim* anim);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_retain_anim"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_retain_anim(ufbx_anim* anim);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_bake_anim"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_baked_anim* ufbx_bake_anim([NativeTypeName("const ufbx_scene *")] ufbx_scene* scene, [NativeTypeName("const ufbx_anim *")] ufbx_anim* anim, [NativeTypeName("const ufbx_bake_opts *")] ufbx_bake_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_retain_baked_anim"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_retain_baked_anim(ufbx_baked_anim* bake);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_free_baked_anim"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_free_baked_anim(ufbx_baked_anim* bake);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_baked_node_by_typed_id"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_baked_node* ufbx_find_baked_node_by_typed_id(ufbx_baked_anim* bake, [NativeTypeName("uint32_t")] uint typed_id);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_baked_node"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_baked_node* ufbx_find_baked_node(ufbx_baked_anim* bake, ufbx_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_baked_element_by_element_id"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_baked_element* ufbx_find_baked_element_by_element_id(ufbx_baked_anim* bake, [NativeTypeName("uint32_t")] uint element_id);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_baked_element"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_baked_element* ufbx_find_baked_element(ufbx_baked_anim* bake, ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_baked_vec3"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_evaluate_baked_vec3(ufbx_baked_vec3_list keyframes, double time);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_baked_quat"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_quat ufbx_evaluate_baked_quat(ufbx_baked_quat_list keyframes, double time);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_get_bone_pose"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_bone_pose* ufbx_get_bone_pose([NativeTypeName("const ufbx_pose *")] ufbx_pose* pose, [NativeTypeName("const ufbx_node *")] ufbx_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_prop_texture_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_texture* ufbx_find_prop_texture_len([NativeTypeName("const ufbx_material *")] ufbx_material* material, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_prop_texture"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_texture* ufbx_find_prop_texture([NativeTypeName("const ufbx_material *")] ufbx_material* material, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_shader_prop_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_string ufbx_find_shader_prop_len([NativeTypeName("const ufbx_shader *")] ufbx_shader* shader, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_shader_prop"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_string ufbx_find_shader_prop([NativeTypeName("const ufbx_shader *")] ufbx_shader* shader, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_shader_prop_bindings_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_shader_prop_binding_list ufbx_find_shader_prop_bindings_len([NativeTypeName("const ufbx_shader *")] ufbx_shader* shader, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_shader_prop_bindings"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_shader_prop_binding_list ufbx_find_shader_prop_bindings([NativeTypeName("const ufbx_shader *")] ufbx_shader* shader, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_shader_texture_input_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_shader_texture_input* ufbx_find_shader_texture_input_len([NativeTypeName("const ufbx_shader_texture *")] ufbx_shader_texture* shader, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_shader_texture_input"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_shader_texture_input* ufbx_find_shader_texture_input([NativeTypeName("const ufbx_shader_texture *")] ufbx_shader_texture* shader, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_coordinate_axes_valid"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_coordinate_axes_valid(ufbx_coordinate_axes axes);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_vec3_normalize"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_vec3_normalize(ufbx_vec3 v);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_quat_dot"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_quat_dot(ufbx_quat a, ufbx_quat b);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_quat_mul"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_quat ufbx_quat_mul(ufbx_quat a, ufbx_quat b);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_quat_normalize"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_quat ufbx_quat_normalize(ufbx_quat q);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_quat_fix_antipodal"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_quat ufbx_quat_fix_antipodal(ufbx_quat q, ufbx_quat reference);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_quat_slerp"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_quat ufbx_quat_slerp(ufbx_quat a, ufbx_quat b, [NativeTypeName("ufbx_real")] float t);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_quat_rotate_vec3"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_quat_rotate_vec3(ufbx_quat q, ufbx_vec3 v);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_quat_to_euler"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_quat_to_euler(ufbx_quat q, ufbx_rotation_order order);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_euler_to_quat"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_quat ufbx_euler_to_quat(ufbx_vec3 v, ufbx_rotation_order order);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_matrix_mul"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_matrix ufbx_matrix_mul([NativeTypeName("const ufbx_matrix *")] ufbx_matrix* a, [NativeTypeName("const ufbx_matrix *")] ufbx_matrix* b);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_matrix_determinant"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_matrix_determinant([NativeTypeName("const ufbx_matrix *")] ufbx_matrix* m);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_matrix_invert"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_matrix ufbx_matrix_invert([NativeTypeName("const ufbx_matrix *")] ufbx_matrix* m);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_matrix_for_normals"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_matrix ufbx_matrix_for_normals([NativeTypeName("const ufbx_matrix *")] ufbx_matrix* m);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_transform_position"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_transform_position([NativeTypeName("const ufbx_matrix *")] ufbx_matrix* m, ufbx_vec3 v);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_transform_direction"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_transform_direction([NativeTypeName("const ufbx_matrix *")] ufbx_matrix* m, ufbx_vec3 v);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_transform_to_matrix"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_matrix ufbx_transform_to_matrix([NativeTypeName("const ufbx_transform *")] ufbx_transform* t);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_matrix_to_transform"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_transform ufbx_matrix_to_transform([NativeTypeName("const ufbx_matrix *")] ufbx_matrix* m);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_get_skin_vertex_matrix"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_matrix ufbx_catch_get_skin_vertex_matrix(ufbx_panic* panic, [NativeTypeName("const ufbx_skin_deformer *")] ufbx_skin_deformer* skin, [NativeTypeName("size_t")] nuint vertex, [NativeTypeName("const ufbx_matrix *")] ufbx_matrix* fallback);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_get_blend_shape_offset_index"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint ufbx_get_blend_shape_offset_index([NativeTypeName("const ufbx_blend_shape *")] ufbx_blend_shape* shape, [NativeTypeName("size_t")] nuint vertex);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_get_blend_shape_vertex_offset"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_get_blend_shape_vertex_offset([NativeTypeName("const ufbx_blend_shape *")] ufbx_blend_shape* shape, [NativeTypeName("size_t")] nuint vertex);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_get_blend_vertex_offset"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_get_blend_vertex_offset([NativeTypeName("const ufbx_blend_deformer *")] ufbx_blend_deformer* blend, [NativeTypeName("size_t")] nuint vertex);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_add_blend_shape_vertex_offsets"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_add_blend_shape_vertex_offsets([NativeTypeName("const ufbx_blend_shape *")] ufbx_blend_shape* shape, ufbx_vec3* vertices, [NativeTypeName("size_t")] nuint num_vertices, [NativeTypeName("ufbx_real")] float weight);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_add_blend_vertex_offsets"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_add_blend_vertex_offsets([NativeTypeName("const ufbx_blend_deformer *")] ufbx_blend_deformer* blend, ufbx_vec3* vertices, [NativeTypeName("size_t")] nuint num_vertices, [NativeTypeName("ufbx_real")] float weight);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_nurbs_basis"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_evaluate_nurbs_basis([NativeTypeName("const ufbx_nurbs_basis *")] ufbx_nurbs_basis* basis, [NativeTypeName("ufbx_real")] float u, [NativeTypeName("ufbx_real *")] float* weights, [NativeTypeName("size_t")] nuint num_weights, [NativeTypeName("ufbx_real *")] float* derivatives, [NativeTypeName("size_t")] nuint num_derivatives);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_nurbs_curve"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_curve_point ufbx_evaluate_nurbs_curve([NativeTypeName("const ufbx_nurbs_curve *")] ufbx_nurbs_curve* curve, [NativeTypeName("ufbx_real")] float u);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_evaluate_nurbs_surface"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_surface_point ufbx_evaluate_nurbs_surface([NativeTypeName("const ufbx_nurbs_surface *")] ufbx_nurbs_surface* surface, [NativeTypeName("ufbx_real")] float u, [NativeTypeName("ufbx_real")] float v);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_tessellate_nurbs_curve"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_line_curve* ufbx_tessellate_nurbs_curve([NativeTypeName("const ufbx_nurbs_curve *")] ufbx_nurbs_curve* curve, [NativeTypeName("const ufbx_tessellate_curve_opts *")] ufbx_tessellate_curve_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_tessellate_nurbs_surface"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_mesh* ufbx_tessellate_nurbs_surface([NativeTypeName("const ufbx_nurbs_surface *")] ufbx_nurbs_surface* surface, [NativeTypeName("const ufbx_tessellate_surface_opts *")] ufbx_tessellate_surface_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_free_line_curve"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_free_line_curve(ufbx_line_curve* curve);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_retain_line_curve"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_retain_line_curve(ufbx_line_curve* curve);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_find_face_index"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint ufbx_find_face_index(ufbx_mesh* mesh, [NativeTypeName("size_t")] nuint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_triangulate_face"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint ufbx_catch_triangulate_face(ufbx_panic* panic, [NativeTypeName("uint32_t *")] uint* indices, [NativeTypeName("size_t")] nuint num_indices, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, ufbx_face face);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_triangulate_face"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint ufbx_triangulate_face([NativeTypeName("uint32_t *")] uint* indices, [NativeTypeName("size_t")] nuint num_indices, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, ufbx_face face);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_compute_topology"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_catch_compute_topology(ufbx_panic* panic, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_compute_topology"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_compute_topology([NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_topo_next_vertex_edge"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint ufbx_catch_topo_next_vertex_edge(ufbx_panic* panic, [NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t")] uint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_topo_next_vertex_edge"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint ufbx_topo_next_vertex_edge([NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t")] uint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_topo_prev_vertex_edge"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint ufbx_catch_topo_prev_vertex_edge(ufbx_panic* panic, [NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t")] uint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_topo_prev_vertex_edge"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("uint32_t")]
    public static extern uint ufbx_topo_prev_vertex_edge([NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t")] uint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_get_weighted_face_normal"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_catch_get_weighted_face_normal(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* positions, ufbx_face face);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_get_weighted_face_normal"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_get_weighted_face_normal([NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* positions, ufbx_face face);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_generate_normal_mapping"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_catch_generate_normal_mapping(ufbx_panic* panic, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t *")] uint* normal_indices, [NativeTypeName("size_t")] nuint num_normal_indices, [NativeTypeName("_Bool")] bool assume_smooth);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_generate_normal_mapping"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_generate_normal_mapping([NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("const ufbx_topo_edge *")] ufbx_topo_edge* topo, [NativeTypeName("size_t")] nuint num_topo, [NativeTypeName("uint32_t *")] uint* normal_indices, [NativeTypeName("size_t")] nuint num_normal_indices, [NativeTypeName("_Bool")] bool assume_smooth);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_compute_normals"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_catch_compute_normals(ufbx_panic* panic, [NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* positions, [NativeTypeName("const uint32_t *")] uint* normal_indices, [NativeTypeName("size_t")] nuint num_normal_indices, ufbx_vec3* normals, [NativeTypeName("size_t")] nuint num_normals);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_compute_normals"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_compute_normals([NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* positions, [NativeTypeName("const uint32_t *")] uint* normal_indices, [NativeTypeName("size_t")] nuint num_normal_indices, ufbx_vec3* normals, [NativeTypeName("size_t")] nuint num_normals);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_subdivide_mesh"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_mesh* ufbx_subdivide_mesh([NativeTypeName("const ufbx_mesh *")] ufbx_mesh* mesh, [NativeTypeName("size_t")] nuint level, [NativeTypeName("const ufbx_subdivide_opts *")] ufbx_subdivide_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_free_mesh"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_free_mesh(ufbx_mesh* mesh);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_retain_mesh"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_retain_mesh(ufbx_mesh* mesh);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_geometry_cache"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_geometry_cache* ufbx_load_geometry_cache([NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("const ufbx_geometry_cache_opts *")] ufbx_geometry_cache_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_load_geometry_cache_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_geometry_cache* ufbx_load_geometry_cache_len([NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("size_t")] nuint filename_len, [NativeTypeName("const ufbx_geometry_cache_opts *")] ufbx_geometry_cache_opts* opts, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_free_geometry_cache"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_free_geometry_cache(ufbx_geometry_cache* cache);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_retain_geometry_cache"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_retain_geometry_cache(ufbx_geometry_cache* cache);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_read_geometry_cache_real"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_read_geometry_cache_real([NativeTypeName("const ufbx_cache_frame *")] ufbx_cache_frame* frame, [NativeTypeName("ufbx_real *")] float* data, [NativeTypeName("size_t")] nuint num_data, [NativeTypeName("const ufbx_geometry_cache_data_opts *")] ufbx_geometry_cache_data_opts* opts);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_read_geometry_cache_vec3"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_read_geometry_cache_vec3([NativeTypeName("const ufbx_cache_frame *")] ufbx_cache_frame* frame, ufbx_vec3* data, [NativeTypeName("size_t")] nuint num_data, [NativeTypeName("const ufbx_geometry_cache_data_opts *")] ufbx_geometry_cache_data_opts* opts);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_sample_geometry_cache_real"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_sample_geometry_cache_real([NativeTypeName("const ufbx_cache_channel *")] ufbx_cache_channel* channel, double time, [NativeTypeName("ufbx_real *")] float* data, [NativeTypeName("size_t")] nuint num_data, [NativeTypeName("const ufbx_geometry_cache_data_opts *")] ufbx_geometry_cache_data_opts* opts);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_sample_geometry_cache_vec3"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_sample_geometry_cache_vec3([NativeTypeName("const ufbx_cache_channel *")] ufbx_cache_channel* channel, double time, ufbx_vec3* data, [NativeTypeName("size_t")] nuint num_data, [NativeTypeName("const ufbx_geometry_cache_data_opts *")] ufbx_geometry_cache_data_opts* opts);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_find_len"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_dom_node* ufbx_dom_find_len([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* parent, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("size_t")] nuint name_len);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_find"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_dom_node* ufbx_dom_find([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* parent, [NativeTypeName("const char *")] sbyte* name);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_generate_indices"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_generate_indices([NativeTypeName("const ufbx_vertex_stream *")] ufbx_vertex_stream* streams, [NativeTypeName("size_t")] nuint num_streams, [NativeTypeName("uint32_t *")] uint* indices, [NativeTypeName("size_t")] nuint num_indices, [NativeTypeName("const ufbx_allocator_opts *")] ufbx_allocator_opts* allocator, ufbx_error* error);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_thread_pool_run_task"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_thread_pool_run_task([NativeTypeName("ufbx_thread_pool_context")] nuint ctx, [NativeTypeName("uint32_t")] uint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_thread_pool_set_user_ptr"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void ufbx_thread_pool_set_user_ptr([NativeTypeName("ufbx_thread_pool_context")] nuint ctx, void* user_ptr);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_thread_pool_get_user_ptr"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void* ufbx_thread_pool_get_user_ptr([NativeTypeName("ufbx_thread_pool_context")] nuint ctx);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_get_vertex_real"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_catch_get_vertex_real(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_real *")] ufbx_vertex_real* v, [NativeTypeName("size_t")] nuint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_get_vertex_vec2"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec2 ufbx_catch_get_vertex_vec2(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec2 *")] ufbx_vertex_vec2* v, [NativeTypeName("size_t")] nuint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_get_vertex_vec3"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec3 ufbx_catch_get_vertex_vec3(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* v, [NativeTypeName("size_t")] nuint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_get_vertex_vec4"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_vec4 ufbx_catch_get_vertex_vec4(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec4 *")] ufbx_vertex_vec4* v, [NativeTypeName("size_t")] nuint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_catch_get_vertex_w_vec3"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("ufbx_real")]
    public static extern float ufbx_catch_get_vertex_w_vec3(ufbx_panic* panic, [NativeTypeName("const ufbx_vertex_vec3 *")] ufbx_vertex_vec3* v, [NativeTypeName("size_t")] nuint index);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_unknown"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_unknown* ufbx_as_unknown([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_node"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_node* ufbx_as_node([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_mesh"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_mesh* ufbx_as_mesh([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_light"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_light* ufbx_as_light([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_camera"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_camera* ufbx_as_camera([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_bone"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_bone* ufbx_as_bone([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_empty"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_empty* ufbx_as_empty([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_line_curve"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_line_curve* ufbx_as_line_curve([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_nurbs_curve"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_nurbs_curve* ufbx_as_nurbs_curve([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_nurbs_surface"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_nurbs_surface* ufbx_as_nurbs_surface([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_nurbs_trim_surface"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_nurbs_trim_surface* ufbx_as_nurbs_trim_surface([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_nurbs_trim_boundary"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_nurbs_trim_boundary* ufbx_as_nurbs_trim_boundary([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_procedural_geometry"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_procedural_geometry* ufbx_as_procedural_geometry([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_stereo_camera"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_stereo_camera* ufbx_as_stereo_camera([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_camera_switcher"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_camera_switcher* ufbx_as_camera_switcher([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_marker"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_marker* ufbx_as_marker([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_lod_group"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_lod_group* ufbx_as_lod_group([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_skin_deformer"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_skin_deformer* ufbx_as_skin_deformer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_skin_cluster"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_skin_cluster* ufbx_as_skin_cluster([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_blend_deformer"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_blend_deformer* ufbx_as_blend_deformer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_blend_channel"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_blend_channel* ufbx_as_blend_channel([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_blend_shape"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_blend_shape* ufbx_as_blend_shape([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_cache_deformer"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_cache_deformer* ufbx_as_cache_deformer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_cache_file"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_cache_file* ufbx_as_cache_file([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_material"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_material* ufbx_as_material([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_texture"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_texture* ufbx_as_texture([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_video"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_video* ufbx_as_video([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_shader"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_shader* ufbx_as_shader([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_shader_binding"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_shader_binding* ufbx_as_shader_binding([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_anim_stack"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_stack* ufbx_as_anim_stack([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_anim_layer"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_layer* ufbx_as_anim_layer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_anim_value"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_value* ufbx_as_anim_value([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_anim_curve"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_anim_curve* ufbx_as_anim_curve([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_display_layer"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_display_layer* ufbx_as_display_layer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_selection_set"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_selection_set* ufbx_as_selection_set([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_selection_node"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_selection_node* ufbx_as_selection_node([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_character"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_character* ufbx_as_character([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_constraint"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_constraint* ufbx_as_constraint([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_audio_layer"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_audio_layer* ufbx_as_audio_layer([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_audio_clip"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_audio_clip* ufbx_as_audio_clip([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_pose"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_pose* ufbx_as_pose([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_as_metadata_object"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_metadata_object* ufbx_as_metadata_object([NativeTypeName("const ufbx_element *")] ufbx_element* element);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_is_array"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("_Bool")]
    public static extern bool ufbx_dom_is_array([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_array_size"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint ufbx_dom_array_size([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_as_int32_list"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_int32_list ufbx_dom_as_int32_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_as_int64_list"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_int64_list ufbx_dom_as_int64_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_as_float_list"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_float_list ufbx_dom_as_float_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_as_double_list"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_double_list ufbx_dom_as_double_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_as_real_list"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_real_list ufbx_dom_as_real_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.ufbx_dom_as_blob_list"]/*' />
    [DllImport("ufbx", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern ufbx_blob_list ufbx_dom_as_blob_list([NativeTypeName("const ufbx_dom_node *")] ufbx_dom_node* node);

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_STDC"]/*' />
    [NativeTypeName("#define UFBX_STDC __STDC_VERSION__")]
    public const int UFBX_STDC = 201112;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CPP"]/*' />
    [NativeTypeName("#define UFBX_CPP 0")]
    public const int UFBX_CPP = 0;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_PLATFORM_MSC"]/*' />
    [NativeTypeName("#define UFBX_PLATFORM_MSC _MSC_VER")]
    public const int UFBX_PLATFORM_MSC = 1952;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_PLATFORM_GNUC"]/*' />
    [NativeTypeName("#define UFBX_PLATFORM_GNUC 0")]
    public const int UFBX_PLATFORM_GNUC = 0;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_CPP11"]/*' />
    [NativeTypeName("#define UFBX_CPP11 0")]
    public const int UFBX_CPP11 = 0;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_ERROR_STACK_MAX_DEPTH"]/*' />
    [NativeTypeName("#define UFBX_ERROR_STACK_MAX_DEPTH 8")]
    public const int UFBX_ERROR_STACK_MAX_DEPTH = 8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_PANIC_MESSAGE_LENGTH"]/*' />
    [NativeTypeName("#define UFBX_PANIC_MESSAGE_LENGTH 128")]
    public const int UFBX_PANIC_MESSAGE_LENGTH = 128;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_ERROR_INFO_LENGTH"]/*' />
    [NativeTypeName("#define UFBX_ERROR_INFO_LENGTH 256")]
    public const int UFBX_ERROR_INFO_LENGTH = 256;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_THREAD_GROUP_COUNT"]/*' />
    [NativeTypeName("#define UFBX_THREAD_GROUP_COUNT 4")]
    public const int UFBX_THREAD_GROUP_COUNT = 4;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_HAS_FORCE_32BIT"]/*' />
    [NativeTypeName("#define UFBX_HAS_FORCE_32BIT 1")]
    public const int UFBX_HAS_FORCE_32BIT = 1;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_HEADER_VERSION"]/*' />
    [NativeTypeName("#define UFBX_HEADER_VERSION ufbx_pack_version(0, 23, 0)")]
    public const uint UFBX_HEADER_VERSION = ((0) * 1000000U + (23) * 1000U + (0));

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_VERSION"]/*' />
    [NativeTypeName("#define UFBX_VERSION UFBX_HEADER_VERSION")]
    public const uint UFBX_VERSION = ((0) * 1000000U + (23) * 1000U + (0));

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_NO_INDEX"]/*' />
    [NativeTypeName("#define UFBX_NO_INDEX ((uint32_t)~0u)")]
    public const uint UFBX_NO_INDEX = ~0U;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_Lcl_Translation"]/*' />
    [NativeTypeName("#define UFBX_Lcl_Translation \"Lcl Translation\"")]
    public static ReadOnlySpan<byte> UFBX_Lcl_Translation => "Lcl Translation"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_Lcl_Rotation"]/*' />
    [NativeTypeName("#define UFBX_Lcl_Rotation \"Lcl Rotation\"")]
    public static ReadOnlySpan<byte> UFBX_Lcl_Rotation => "Lcl Rotation"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_Lcl_Scaling"]/*' />
    [NativeTypeName("#define UFBX_Lcl_Scaling \"Lcl Scaling\"")]
    public static ReadOnlySpan<byte> UFBX_Lcl_Scaling => "Lcl Scaling"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_RotationOrder"]/*' />
    [NativeTypeName("#define UFBX_RotationOrder \"RotationOrder\"")]
    public static ReadOnlySpan<byte> UFBX_RotationOrder => "RotationOrder"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_ScalingPivot"]/*' />
    [NativeTypeName("#define UFBX_ScalingPivot \"ScalingPivot\"")]
    public static ReadOnlySpan<byte> UFBX_ScalingPivot => "ScalingPivot"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_RotationPivot"]/*' />
    [NativeTypeName("#define UFBX_RotationPivot \"RotationPivot\"")]
    public static ReadOnlySpan<byte> UFBX_RotationPivot => "RotationPivot"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_ScalingOffset"]/*' />
    [NativeTypeName("#define UFBX_ScalingOffset \"ScalingOffset\"")]
    public static ReadOnlySpan<byte> UFBX_ScalingOffset => "ScalingOffset"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_RotationOffset"]/*' />
    [NativeTypeName("#define UFBX_RotationOffset \"RotationOffset\"")]
    public static ReadOnlySpan<byte> UFBX_RotationOffset => "RotationOffset"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_PreRotation"]/*' />
    [NativeTypeName("#define UFBX_PreRotation \"PreRotation\"")]
    public static ReadOnlySpan<byte> UFBX_PreRotation => "PreRotation"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_PostRotation"]/*' />
    [NativeTypeName("#define UFBX_PostRotation \"PostRotation\"")]
    public static ReadOnlySpan<byte> UFBX_PostRotation => "PostRotation"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_Visibility"]/*' />
    [NativeTypeName("#define UFBX_Visibility \"Visibility\"")]
    public static ReadOnlySpan<byte> UFBX_Visibility => "Visibility"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_Weight"]/*' />
    [NativeTypeName("#define UFBX_Weight \"Weight\"")]
    public static ReadOnlySpan<byte> UFBX_Weight => "Weight"u8;

    /// <include file='Api.xml' path='doc/member[@name="Api.UFBX_DeformPercent"]/*' />
    [NativeTypeName("#define UFBX_DeformPercent \"DeformPercent\"")]
    public static ReadOnlySpan<byte> UFBX_DeformPercent => "DeformPercent"u8;
}
