namespace Ghost.Ufbx
{
    public partial struct ufbx_thread_opts
    {
        public ufbx_thread_pool pool;

        [NativeTypeName("size_t")]
        public nuint num_tasks;

        [NativeTypeName("size_t")]
        public nuint memory_limit;
    }
}
