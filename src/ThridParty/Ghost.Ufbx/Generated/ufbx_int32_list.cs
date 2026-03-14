namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_int32_list
    {
        [NativeTypeName("int32_t *")]
        public int* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
