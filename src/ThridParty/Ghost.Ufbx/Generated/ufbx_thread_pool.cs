namespace Ghost.Ufbx
{
    /// <include file='ufbx_thread_pool.xml' path='doc/member[@name="ufbx_thread_pool"]/*' />
    public unsafe partial struct ufbx_thread_pool
    {
        /// <include file='ufbx_thread_pool.xml' path='doc/member[@name="ufbx_thread_pool.init_fn"]/*' />
        [NativeTypeName("ufbx_thread_pool_init_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, ufbx_thread_pool_info*, bool> init_fn;

        /// <include file='ufbx_thread_pool.xml' path='doc/member[@name="ufbx_thread_pool.run_fn"]/*' />
        [NativeTypeName("ufbx_thread_pool_run_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, uint, uint, uint, void> run_fn;

        /// <include file='ufbx_thread_pool.xml' path='doc/member[@name="ufbx_thread_pool.wait_fn"]/*' />
        [NativeTypeName("ufbx_thread_pool_wait_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, uint, uint, void> wait_fn;

        /// <include file='ufbx_thread_pool.xml' path='doc/member[@name="ufbx_thread_pool.free_fn"]/*' />
        [NativeTypeName("ufbx_thread_pool_free_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, void> free_fn;

        /// <include file='ufbx_thread_pool.xml' path='doc/member[@name="ufbx_thread_pool.user"]/*' />
        public void* user;
    }
}
