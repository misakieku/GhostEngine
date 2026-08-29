namespace Ghost.Ufbx
{
    /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input"]/*' />
    public unsafe partial struct ufbx_inflate_input
    {
        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.total_size"]/*' />
        [NativeTypeName("size_t")]
        public nuint total_size;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.data"]/*' />
        [NativeTypeName("const void *")]
        public void* data;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.data_size"]/*' />
        [NativeTypeName("size_t")]
        public nuint data_size;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.buffer"]/*' />
        public void* buffer;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.buffer_size"]/*' />
        [NativeTypeName("size_t")]
        public nuint buffer_size;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.read_fn"]/*' />
        [NativeTypeName("ufbx_read_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint> read_fn;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.read_user"]/*' />
        public void* read_user;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.progress_cb"]/*' />
        public ufbx_progress_cb progress_cb;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.progress_interval_hint"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong progress_interval_hint;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.progress_size_before"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong progress_size_before;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.progress_size_after"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong progress_size_after;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.no_header"]/*' />
        [NativeTypeName("_Bool")]
        public bool no_header;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.no_checksum"]/*' />
        [NativeTypeName("_Bool")]
        public bool no_checksum;

        /// <include file='ufbx_inflate_input.xml' path='doc/member[@name="ufbx_inflate_input.internal_fast_bits"]/*' />
        [NativeTypeName("size_t")]
        public nuint internal_fast_bits;
    }
}
