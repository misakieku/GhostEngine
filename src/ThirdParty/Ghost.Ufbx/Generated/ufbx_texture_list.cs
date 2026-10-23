namespace Ghost.Ufbx
{
    /// <include file='ufbx_texture_list.xml' path='doc/member[@name="ufbx_texture_list"]/*' />
    public unsafe partial struct ufbx_texture_list
    {
        /// <include file='ufbx_texture_list.xml' path='doc/member[@name="ufbx_texture_list.data"]/*' />
        public ufbx_texture** data;

        /// <include file='ufbx_texture_list.xml' path='doc/member[@name="ufbx_texture_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
