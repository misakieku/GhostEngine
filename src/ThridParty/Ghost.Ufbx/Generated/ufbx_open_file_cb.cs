namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_open_file_cb
    {
        [NativeTypeName("ufbx_open_file_fn *")]
        public delegate* unmanaged[Cdecl]<void*, ufbx_stream*, sbyte*, nuint, ufbx_open_file_info*, bool> fn;

        public void* user;
    }
}
