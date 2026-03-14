namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_const_uint32_list
    {
        [NativeTypeName("const uint32_t *")]
        public uint* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
