namespace Ghost.Ufbx
{
    /// <include file='ufbx_vertex_real.xml' path='doc/member[@name="ufbx_vertex_real"]/*' />
    public partial struct ufbx_vertex_real
    {
        /// <include file='ufbx_vertex_real.xml' path='doc/member[@name="ufbx_vertex_real.exists"]/*' />
        [NativeTypeName("_Bool")]
        public bool exists;

        /// <include file='ufbx_vertex_real.xml' path='doc/member[@name="ufbx_vertex_real.values"]/*' />
        public ufbx_real_list values;

        /// <include file='ufbx_vertex_real.xml' path='doc/member[@name="ufbx_vertex_real.indices"]/*' />
        public ufbx_uint32_list indices;

        /// <include file='ufbx_vertex_real.xml' path='doc/member[@name="ufbx_vertex_real.value_reals"]/*' />
        [NativeTypeName("size_t")]
        public nuint value_reals;

        /// <include file='ufbx_vertex_real.xml' path='doc/member[@name="ufbx_vertex_real.unique_per_vertex"]/*' />
        [NativeTypeName("_Bool")]
        public bool unique_per_vertex;

        /// <include file='ufbx_vertex_real.xml' path='doc/member[@name="ufbx_vertex_real.values_w"]/*' />
        public ufbx_real_list values_w;
    }
}
