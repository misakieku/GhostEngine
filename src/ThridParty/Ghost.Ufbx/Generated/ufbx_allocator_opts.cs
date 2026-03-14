namespace Ghost.Ufbx
{
    public partial struct ufbx_allocator_opts
    {
        public ufbx_allocator allocator;

        [NativeTypeName("size_t")]
        public nuint memory_limit;

        [NativeTypeName("size_t")]
        public nuint allocation_limit;

        [NativeTypeName("size_t")]
        public nuint huge_threshold;

        [NativeTypeName("size_t")]
        public nuint max_chunk_size;
    }
}
