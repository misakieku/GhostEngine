namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_void_list
    {
        public void* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
