namespace Ghost.Ufbx
{
    /// <include file='ufbx_progress_cb.xml' path='doc/member[@name="ufbx_progress_cb"]/*' />
    public unsafe partial struct ufbx_progress_cb
    {
        /// <include file='ufbx_progress_cb.xml' path='doc/member[@name="ufbx_progress_cb.fn"]/*' />
        [NativeTypeName("ufbx_progress_fn *")]
        public delegate* unmanaged[Cdecl]<void*, ufbx_progress*, ufbx_progress_result> fn;

        /// <include file='ufbx_progress_cb.xml' path='doc/member[@name="ufbx_progress_cb.user"]/*' />
        public void* user;
    }
}
