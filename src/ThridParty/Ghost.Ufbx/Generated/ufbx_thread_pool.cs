namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_thread_pool
    {
        [NativeTypeName("ufbx_thread_pool_init_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, ufbx_thread_pool_info*, bool> init_fn;

        [NativeTypeName("ufbx_thread_pool_run_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, uint, uint, uint, void> run_fn;

        [NativeTypeName("ufbx_thread_pool_wait_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, uint, uint, void> wait_fn;

        [NativeTypeName("ufbx_thread_pool_free_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, void> free_fn;

        public void* user;
    }
}
