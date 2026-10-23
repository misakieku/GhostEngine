namespace Ghost.Ufbx
{
    /// <include file='ufbx_allocator_opts.xml' path='doc/member[@name="ufbx_allocator_opts"]/*' />
    public partial struct ufbx_allocator_opts
    {
        /// <include file='ufbx_allocator_opts.xml' path='doc/member[@name="ufbx_allocator_opts.allocator"]/*' />
        public ufbx_allocator allocator;

        /// <include file='ufbx_allocator_opts.xml' path='doc/member[@name="ufbx_allocator_opts.memory_limit"]/*' />
        [NativeTypeName("size_t")]
        public nuint memory_limit;

        /// <include file='ufbx_allocator_opts.xml' path='doc/member[@name="ufbx_allocator_opts.allocation_limit"]/*' />
        [NativeTypeName("size_t")]
        public nuint allocation_limit;

        /// <include file='ufbx_allocator_opts.xml' path='doc/member[@name="ufbx_allocator_opts.huge_threshold"]/*' />
        [NativeTypeName("size_t")]
        public nuint huge_threshold;

        /// <include file='ufbx_allocator_opts.xml' path='doc/member[@name="ufbx_allocator_opts.max_chunk_size"]/*' />
        [NativeTypeName("size_t")]
        public nuint max_chunk_size;
    }
}
