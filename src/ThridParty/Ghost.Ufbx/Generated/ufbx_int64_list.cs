namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_int64_list
    {
        [NativeTypeName("int64_t *")]
        public long* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
