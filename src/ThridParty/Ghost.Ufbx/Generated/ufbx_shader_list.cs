namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_shader_list
    {
        public ufbx_shader** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
