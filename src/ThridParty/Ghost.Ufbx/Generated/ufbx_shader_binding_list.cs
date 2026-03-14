namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_shader_binding_list
    {
        public ufbx_shader_binding** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
