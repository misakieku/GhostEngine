namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_texture_file_list
    {
        public ufbx_texture_file* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
