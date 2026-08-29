namespace Ghost.Ufbx
{
    /// <include file='ufbx_texture_file_list.xml' path='doc/member[@name="ufbx_texture_file_list"]/*' />
    public unsafe partial struct ufbx_texture_file_list
    {
        /// <include file='ufbx_texture_file_list.xml' path='doc/member[@name="ufbx_texture_file_list.data"]/*' />
        public ufbx_texture_file* data;

        /// <include file='ufbx_texture_file_list.xml' path='doc/member[@name="ufbx_texture_file_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
