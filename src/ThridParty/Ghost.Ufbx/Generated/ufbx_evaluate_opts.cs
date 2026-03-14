namespace Ghost.Ufbx
{
    public partial struct ufbx_evaluate_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_allocator_opts temp_allocator;

        public ufbx_allocator_opts result_allocator;

        [NativeTypeName("_Bool")]
        public bool evaluate_skinning;

        [NativeTypeName("_Bool")]
        public bool evaluate_caches;

        [NativeTypeName("uint32_t")]
        public uint evaluate_flags;

        [NativeTypeName("_Bool")]
        public bool load_external_files;

        public ufbx_open_file_cb open_file_cb;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
