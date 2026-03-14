namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_allocator
    {
        [NativeTypeName("ufbx_alloc_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, void*> alloc_fn;

        [NativeTypeName("ufbx_realloc_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> realloc_fn;

        [NativeTypeName("ufbx_free_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, void> free_fn;

        [NativeTypeName("ufbx_free_allocator_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void> free_allocator_fn;

        public void* user;
    }
}
