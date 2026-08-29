namespace Ghost.Ufbx
{
    /// <include file='ufbx_open_memory_opts.xml' path='doc/member[@name="ufbx_open_memory_opts"]/*' />
    public partial struct ufbx_open_memory_opts
    {
        /// <include file='ufbx_open_memory_opts.xml' path='doc/member[@name="ufbx_open_memory_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_open_memory_opts.xml' path='doc/member[@name="ufbx_open_memory_opts.allocator"]/*' />
        public ufbx_allocator_opts allocator;

        /// <include file='ufbx_open_memory_opts.xml' path='doc/member[@name="ufbx_open_memory_opts.no_copy"]/*' />
        [NativeTypeName("_Bool")]
        public bool no_copy;

        /// <include file='ufbx_open_memory_opts.xml' path='doc/member[@name="ufbx_open_memory_opts.close_cb"]/*' />
        public ufbx_close_memory_cb close_cb;

        /// <include file='ufbx_open_memory_opts.xml' path='doc/member[@name="ufbx_open_memory_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
