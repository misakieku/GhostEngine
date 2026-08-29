namespace Ghost.Ufbx
{
    /// <include file='ufbx_open_file_opts.xml' path='doc/member[@name="ufbx_open_file_opts"]/*' />
    public partial struct ufbx_open_file_opts
    {
        /// <include file='ufbx_open_file_opts.xml' path='doc/member[@name="ufbx_open_file_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_open_file_opts.xml' path='doc/member[@name="ufbx_open_file_opts.allocator"]/*' />
        public ufbx_allocator_opts allocator;

        /// <include file='ufbx_open_file_opts.xml' path='doc/member[@name="ufbx_open_file_opts.filename_null_terminated"]/*' />
        [NativeTypeName("_Bool")]
        public bool filename_null_terminated;

        /// <include file='ufbx_open_file_opts.xml' path='doc/member[@name="ufbx_open_file_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
