using System.Runtime.CompilerServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_metadata
    {
        public ufbx_warning_list warnings;

        [NativeTypeName("_Bool")]
        public bool ascii;

        [NativeTypeName("uint32_t")]
        public uint version;

        public ufbx_file_format file_format;

        [NativeTypeName("_Bool")]
        public bool may_contain_no_index;

        [NativeTypeName("_Bool")]
        public bool may_contain_missing_vertex_position;

        [NativeTypeName("_Bool")]
        public bool may_contain_broken_elements;

        [NativeTypeName("_Bool")]
        public bool is_unsafe;

        [NativeTypeName("_Bool[15]")]
        public _has_warning_e__FixedBuffer has_warning;

        public ufbx_string creator;

        [NativeTypeName("_Bool")]
        public bool big_endian;

        public ufbx_string filename;

        public ufbx_string relative_root;

        public ufbx_blob raw_filename;

        public ufbx_blob raw_relative_root;

        public ufbx_exporter exporter;

        [NativeTypeName("uint32_t")]
        public uint exporter_version;

        public ufbx_props scene_props;

        public ufbx_application original_application;

        public ufbx_application latest_application;

        public ufbx_thumbnail thumbnail;

        [NativeTypeName("_Bool")]
        public bool geometry_ignored;

        [NativeTypeName("_Bool")]
        public bool animation_ignored;

        [NativeTypeName("_Bool")]
        public bool embedded_ignored;

        [NativeTypeName("size_t")]
        public nuint max_face_triangles;

        [NativeTypeName("size_t")]
        public nuint result_memory_used;

        [NativeTypeName("size_t")]
        public nuint temp_memory_used;

        [NativeTypeName("size_t")]
        public nuint result_allocs;

        [NativeTypeName("size_t")]
        public nuint temp_allocs;

        [NativeTypeName("size_t")]
        public nuint element_buffer_size;

        [NativeTypeName("size_t")]
        public nuint num_shader_textures;

        [NativeTypeName("ufbx_real")]
        public float bone_prop_size_unit;

        [NativeTypeName("_Bool")]
        public bool bone_prop_limb_length_relative;

        [NativeTypeName("ufbx_real")]
        public float ortho_size_unit;

        [NativeTypeName("int64_t")]
        public long ktime_second;

        public ufbx_string original_file_path;

        public ufbx_blob raw_original_file_path;

        public ufbx_space_conversion space_conversion;

        public ufbx_geometry_transform_handling geometry_transform_handling;

        public ufbx_inherit_mode_handling inherit_mode_handling;

        public ufbx_pivot_handling pivot_handling;

        public ufbx_mirror_axis handedness_conversion_axis;

        public ufbx_quat root_rotation;

        [NativeTypeName("ufbx_real")]
        public float root_scale;

        public ufbx_mirror_axis mirror_axis;

        [NativeTypeName("ufbx_real")]
        public float geometry_scale;

        [InlineArray(15)]
        public partial struct _has_warning_e__FixedBuffer
        {
            public bool e0;
        }
    }
}
