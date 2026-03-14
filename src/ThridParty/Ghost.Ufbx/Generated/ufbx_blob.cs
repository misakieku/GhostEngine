namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_blob
    {
        [NativeTypeName("const void *")]
        public void* data;

        [NativeTypeName("size_t")]
        public nuint size;
    }
}
