namespace Ghost.Ufbx
{
    /// <include file='ufbx_close_memory_cb.xml' path='doc/member[@name="ufbx_close_memory_cb"]/*' />
    public unsafe partial struct ufbx_close_memory_cb
    {
        /// <include file='ufbx_close_memory_cb.xml' path='doc/member[@name="ufbx_close_memory_cb.fn"]/*' />
        [NativeTypeName("ufbx_close_memory_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, void> fn;

        /// <include file='ufbx_close_memory_cb.xml' path='doc/member[@name="ufbx_close_memory_cb.user"]/*' />
        public void* user;
    }
}
