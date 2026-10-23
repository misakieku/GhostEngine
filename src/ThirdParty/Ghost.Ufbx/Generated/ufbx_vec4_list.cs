namespace Ghost.Ufbx
{
    /// <include file='ufbx_vec4_list.xml' path='doc/member[@name="ufbx_vec4_list"]/*' />
    public unsafe partial struct ufbx_vec4_list
    {
        /// <include file='ufbx_vec4_list.xml' path='doc/member[@name="ufbx_vec4_list.data"]/*' />
        public ufbx_vec4* data;

        /// <include file='ufbx_vec4_list.xml' path='doc/member[@name="ufbx_vec4_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
