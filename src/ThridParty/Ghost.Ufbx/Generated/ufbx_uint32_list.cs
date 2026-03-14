namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_uint32_list
    {
        [NativeTypeName("uint32_t *")]
        public uint* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
