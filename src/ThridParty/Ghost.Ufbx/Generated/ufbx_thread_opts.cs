namespace Ghost.Ufbx
{
    /// <include file='ufbx_thread_opts.xml' path='doc/member[@name="ufbx_thread_opts"]/*' />
    public partial struct ufbx_thread_opts
    {
        /// <include file='ufbx_thread_opts.xml' path='doc/member[@name="ufbx_thread_opts.pool"]/*' />
        public ufbx_thread_pool pool;

        /// <include file='ufbx_thread_opts.xml' path='doc/member[@name="ufbx_thread_opts.num_tasks"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_tasks;

        /// <include file='ufbx_thread_opts.xml' path='doc/member[@name="ufbx_thread_opts.memory_limit"]/*' />
        [NativeTypeName("size_t")]
        public nuint memory_limit;
    }
}
