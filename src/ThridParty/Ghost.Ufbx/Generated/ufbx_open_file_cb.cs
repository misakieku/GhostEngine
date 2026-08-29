namespace Ghost.Ufbx
{
    /// <include file='ufbx_open_file_cb.xml' path='doc/member[@name="ufbx_open_file_cb"]/*' />
    public unsafe partial struct ufbx_open_file_cb
    {
        /// <include file='ufbx_open_file_cb.xml' path='doc/member[@name="ufbx_open_file_cb.fn"]/*' />
        [NativeTypeName("ufbx_open_file_fn *")]
        public delegate* unmanaged[Cdecl]<void*, ufbx_stream*, sbyte*, nuint, ufbx_open_file_info*, bool> fn;

        /// <include file='ufbx_open_file_cb.xml' path='doc/member[@name="ufbx_open_file_cb.user"]/*' />
        public void* user;
    }
}
