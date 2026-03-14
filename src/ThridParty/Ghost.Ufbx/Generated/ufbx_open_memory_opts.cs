namespace Ghost.Ufbx
{
    public partial struct ufbx_open_memory_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_allocator_opts allocator;

        [NativeTypeName("_Bool")]
        public bool no_copy;

        public ufbx_close_memory_cb close_cb;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
