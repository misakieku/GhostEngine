namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_stream
    {
        [NativeTypeName("ufbx_read_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint> read_fn;

        [NativeTypeName("ufbx_skip_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, bool> skip_fn;

        [NativeTypeName("ufbx_size_fn *")]
        public delegate* unmanaged[Cdecl]<void*, ulong> size_fn;

        [NativeTypeName("ufbx_close_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void> close_fn;

        public void* user;
    }
}
