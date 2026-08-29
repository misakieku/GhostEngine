namespace Ghost.Ufbx
{
    /// <include file='ufbx_vertex_vec4.xml' path='doc/member[@name="ufbx_vertex_vec4"]/*' />
    public partial struct ufbx_vertex_vec4
    {
        /// <include file='ufbx_vertex_vec4.xml' path='doc/member[@name="ufbx_vertex_vec4.exists"]/*' />
        [NativeTypeName("_Bool")]
        public bool exists;

        /// <include file='ufbx_vertex_vec4.xml' path='doc/member[@name="ufbx_vertex_vec4.values"]/*' />
        public ufbx_vec4_list values;

        /// <include file='ufbx_vertex_vec4.xml' path='doc/member[@name="ufbx_vertex_vec4.indices"]/*' />
        public ufbx_uint32_list indices;

        /// <include file='ufbx_vertex_vec4.xml' path='doc/member[@name="ufbx_vertex_vec4.value_reals"]/*' />
        [NativeTypeName("size_t")]
        public nuint value_reals;

        /// <include file='ufbx_vertex_vec4.xml' path='doc/member[@name="ufbx_vertex_vec4.unique_per_vertex"]/*' />
        [NativeTypeName("_Bool")]
        public bool unique_per_vertex;

        /// <include file='ufbx_vertex_vec4.xml' path='doc/member[@name="ufbx_vertex_vec4.values_w"]/*' />
        public ufbx_real_list values_w;
    }
}
