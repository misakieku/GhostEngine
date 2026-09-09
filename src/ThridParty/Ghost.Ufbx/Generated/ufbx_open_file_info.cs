namespace Ghost.Ufbx
{
    /// <include file='ufbx_open_file_info.xml' path='doc/member[@name="ufbx_open_file_info"]/*' />
    public partial struct ufbx_open_file_info
    {
        /// <include file='ufbx_open_file_info.xml' path='doc/member[@name="ufbx_open_file_info.context"]/*' />
        [NativeTypeName("ufbx_open_file_context")]
        public nuint context;

        /// <include file='ufbx_open_file_info.xml' path='doc/member[@name="ufbx_open_file_info.type"]/*' />
        public ufbx_open_file_type type;

        /// <include file='ufbx_open_file_info.xml' path='doc/member[@name="ufbx_open_file_info.original_filename"]/*' />
        public ufbx_blob original_filename;
    }
}
