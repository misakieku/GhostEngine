namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_inflate_input
    {
        [NativeTypeName("size_t")]
        public nuint total_size;

        [NativeTypeName("const void *")]
        public void* data;

        [NativeTypeName("size_t")]
        public nuint data_size;

        public void* buffer;

        [NativeTypeName("size_t")]
        public nuint buffer_size;

        [NativeTypeName("ufbx_read_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint> read_fn;

        public void* read_user;

        public ufbx_progress_cb progress_cb;

        [NativeTypeName("uint64_t")]
        public ulong progress_interval_hint;

        [NativeTypeName("uint64_t")]
        public ulong progress_size_before;

        [NativeTypeName("uint64_t")]
        public ulong progress_size_after;

        [NativeTypeName("_Bool")]
        public bool no_header;

        [NativeTypeName("_Bool")]
        public bool no_checksum;

        [NativeTypeName("size_t")]
        public nuint internal_fast_bits;
    }
}
