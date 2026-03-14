namespace Ghost.Ufbx
{
    public partial struct ufbx_open_file_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_allocator_opts allocator;

        [NativeTypeName("_Bool")]
        public bool filename_null_terminated;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
