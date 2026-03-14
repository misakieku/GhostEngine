namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_progress_cb
    {
        [NativeTypeName("ufbx_progress_fn *")]
        public delegate* unmanaged[Cdecl]<void*, ufbx_progress*, ufbx_progress_result> fn;

        public void* user;
    }
}
