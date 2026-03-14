namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_uv_set_list
    {
        public ufbx_uv_set* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
