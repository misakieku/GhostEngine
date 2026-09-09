namespace Ghost.Ufbx
{
    /// <include file='ufbx_vertex_attrib.xml' path='doc/member[@name="ufbx_vertex_attrib"]/*' />
    public partial struct ufbx_vertex_attrib
    {
        /// <include file='ufbx_vertex_attrib.xml' path='doc/member[@name="ufbx_vertex_attrib.exists"]/*' />
        [NativeTypeName("_Bool")]
        public bool exists;

        /// <include file='ufbx_vertex_attrib.xml' path='doc/member[@name="ufbx_vertex_attrib.values"]/*' />
        public ufbx_void_list values;

        /// <include file='ufbx_vertex_attrib.xml' path='doc/member[@name="ufbx_vertex_attrib.indices"]/*' />
        public ufbx_uint32_list indices;

        /// <include file='ufbx_vertex_attrib.xml' path='doc/member[@name="ufbx_vertex_attrib.value_reals"]/*' />
        [NativeTypeName("size_t")]
        public nuint value_reals;

        /// <include file='ufbx_vertex_attrib.xml' path='doc/member[@name="ufbx_vertex_attrib.unique_per_vertex"]/*' />
        [NativeTypeName("_Bool")]
        public bool unique_per_vertex;

        /// <include file='ufbx_vertex_attrib.xml' path='doc/member[@name="ufbx_vertex_attrib.values_w"]/*' />
        public ufbx_real_list values_w;
    }
}
