using System.Runtime.CompilerServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata"]/*' />
    public partial struct ufbx_metadata
    {
        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.warnings"]/*' />
        public ufbx_warning_list warnings;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.ascii"]/*' />
        [NativeTypeName("_Bool")]
        public bool ascii;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.version"]/*' />
        [NativeTypeName("uint32_t")]
        public uint version;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.file_format"]/*' />
        public ufbx_file_format file_format;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.may_contain_no_index"]/*' />
        [NativeTypeName("_Bool")]
        public bool may_contain_no_index;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.may_contain_missing_vertex_position"]/*' />
        [NativeTypeName("_Bool")]
        public bool may_contain_missing_vertex_position;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.may_contain_broken_elements"]/*' />
        [NativeTypeName("_Bool")]
        public bool may_contain_broken_elements;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.is_unsafe"]/*' />
        [NativeTypeName("_Bool")]
        public bool is_unsafe;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.has_warning"]/*' />
        [NativeTypeName("_Bool[15]")]
        public _has_warning_e__FixedBuffer has_warning;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.creator"]/*' />
        public ufbx_string creator;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.big_endian"]/*' />
        [NativeTypeName("_Bool")]
        public bool big_endian;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.filename"]/*' />
        public ufbx_string filename;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.relative_root"]/*' />
        public ufbx_string relative_root;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.raw_filename"]/*' />
        public ufbx_blob raw_filename;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.raw_relative_root"]/*' />
        public ufbx_blob raw_relative_root;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.exporter"]/*' />
        public ufbx_exporter exporter;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.exporter_version"]/*' />
        [NativeTypeName("uint32_t")]
        public uint exporter_version;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.scene_props"]/*' />
        public ufbx_props scene_props;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.original_application"]/*' />
        public ufbx_application original_application;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.latest_application"]/*' />
        public ufbx_application latest_application;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.thumbnail"]/*' />
        public ufbx_thumbnail thumbnail;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.geometry_ignored"]/*' />
        [NativeTypeName("_Bool")]
        public bool geometry_ignored;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.animation_ignored"]/*' />
        [NativeTypeName("_Bool")]
        public bool animation_ignored;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.embedded_ignored"]/*' />
        [NativeTypeName("_Bool")]
        public bool embedded_ignored;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.max_face_triangles"]/*' />
        [NativeTypeName("size_t")]
        public nuint max_face_triangles;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.result_memory_used"]/*' />
        [NativeTypeName("size_t")]
        public nuint result_memory_used;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.temp_memory_used"]/*' />
        [NativeTypeName("size_t")]
        public nuint temp_memory_used;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.result_allocs"]/*' />
        [NativeTypeName("size_t")]
        public nuint result_allocs;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.temp_allocs"]/*' />
        [NativeTypeName("size_t")]
        public nuint temp_allocs;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.element_buffer_size"]/*' />
        [NativeTypeName("size_t")]
        public nuint element_buffer_size;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.num_shader_textures"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_shader_textures;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.bone_prop_size_unit"]/*' />
        [NativeTypeName("ufbx_real")]
        public float bone_prop_size_unit;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.bone_prop_limb_length_relative"]/*' />
        [NativeTypeName("_Bool")]
        public bool bone_prop_limb_length_relative;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.ortho_size_unit"]/*' />
        [NativeTypeName("ufbx_real")]
        public float ortho_size_unit;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.ktime_second"]/*' />
        [NativeTypeName("int64_t")]
        public long ktime_second;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.original_file_path"]/*' />
        public ufbx_string original_file_path;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.raw_original_file_path"]/*' />
        public ufbx_blob raw_original_file_path;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.space_conversion"]/*' />
        public ufbx_space_conversion space_conversion;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.geometry_transform_handling"]/*' />
        public ufbx_geometry_transform_handling geometry_transform_handling;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.inherit_mode_handling"]/*' />
        public ufbx_inherit_mode_handling inherit_mode_handling;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.pivot_handling"]/*' />
        public ufbx_pivot_handling pivot_handling;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.handedness_conversion_axis"]/*' />
        public ufbx_mirror_axis handedness_conversion_axis;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.root_rotation"]/*' />
        public ufbx_quat root_rotation;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.root_scale"]/*' />
        [NativeTypeName("ufbx_real")]
        public float root_scale;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.mirror_axis"]/*' />
        public ufbx_mirror_axis mirror_axis;

        /// <include file='ufbx_metadata.xml' path='doc/member[@name="ufbx_metadata.geometry_scale"]/*' />
        [NativeTypeName("ufbx_real")]
        public float geometry_scale;

        /// <include file='_has_warning_e__FixedBuffer.xml' path='doc/member[@name="_has_warning_e__FixedBuffer"]/*' />
        [InlineArray(15)]
        public partial struct _has_warning_e__FixedBuffer
        {
            public bool e0;
        }
    }
}
