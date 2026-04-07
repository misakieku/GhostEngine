namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_vec2_list
    {
        public ufbx_vec2* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
