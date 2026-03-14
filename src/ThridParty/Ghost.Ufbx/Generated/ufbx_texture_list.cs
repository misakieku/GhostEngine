namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_texture_list
    {
        public ufbx_texture** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
