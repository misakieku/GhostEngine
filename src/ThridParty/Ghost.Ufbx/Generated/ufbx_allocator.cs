namespace Ghost.Ufbx
{
    /// <include file='ufbx_allocator.xml' path='doc/member[@name="ufbx_allocator"]/*' />
    public unsafe partial struct ufbx_allocator
    {
        /// <include file='ufbx_allocator.xml' path='doc/member[@name="ufbx_allocator.alloc_fn"]/*' />
        [NativeTypeName("ufbx_alloc_fn *")]
        public delegate* unmanaged[Cdecl]<void*, nuint, void*> alloc_fn;

        /// <include file='ufbx_allocator.xml' path='doc/member[@name="ufbx_allocator.realloc_fn"]/*' />
        [NativeTypeName("ufbx_realloc_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> realloc_fn;

        /// <include file='ufbx_allocator.xml' path='doc/member[@name="ufbx_allocator.free_fn"]/*' />
        [NativeTypeName("ufbx_free_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, void> free_fn;

        /// <include file='ufbx_allocator.xml' path='doc/member[@name="ufbx_allocator.free_allocator_fn"]/*' />
        [NativeTypeName("ufbx_free_allocator_fn *")]
        public delegate* unmanaged[Cdecl]<void*, void> free_allocator_fn;

        /// <include file='ufbx_allocator.xml' path='doc/member[@name="ufbx_allocator.user"]/*' />
        public void* user;
    }
}
