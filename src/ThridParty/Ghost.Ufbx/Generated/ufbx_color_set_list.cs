namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_color_set_list
    {
        public ufbx_color_set* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
