namespace Ghost.Ufbx
{
    /// <include file='ufbx_stream.xml' path='doc/member[@name="ufbx_stream"]/*' />
    public unsafe partial struct ufbx_stream
    {
        /// <include file='ufbx_stream.xml' path='doc/member[@name="ufbx_stream.read_fn"]/*' />
        [NativeTypeName("ufbx_read_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint> read_fn;

        /// <include file='ufbx_stream.xml' path='doc/member[@name="ufbx_stream.skip_fn"]/*' />
        [NativeTypeName("ufbx_skip_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, bool> skip_fn;

        /// <include file='ufbx_stream.xml' path='doc/member[@name="ufbx_stream.size_fn"]/*' />
        [NativeTypeName("ufbx_size_fn *")]
        public delegate* unmanaged[Cdecl]<void*, ulong> size_fn;

        /// <include file='ufbx_stream.xml' path='doc/member[@name="ufbx_stream.close_fn"]/*' />
        [NativeTypeName("ufbx_close_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void> close_fn;

        /// <include file='ufbx_stream.xml' path='doc/member[@name="ufbx_stream.user"]/*' />
        public void* user;
    }
}
