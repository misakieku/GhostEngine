namespace Ghost.Ufbx
{
    /// <include file='ufbx_vec2_list.xml' path='doc/member[@name="ufbx_vec2_list"]/*' />
    public unsafe partial struct ufbx_vec2_list
    {
        /// <include file='ufbx_vec2_list.xml' path='doc/member[@name="ufbx_vec2_list.data"]/*' />
        public ufbx_vec2* data;

        /// <include file='ufbx_vec2_list.xml' path='doc/member[@name="ufbx_vec2_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
