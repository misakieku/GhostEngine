namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_close_memory_cb
    {
        [NativeTypeName("ufbx_close_memory_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, void> fn;

        public void* user;
    }
}
