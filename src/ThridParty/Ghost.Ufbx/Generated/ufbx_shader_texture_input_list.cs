namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_shader_texture_input_list
    {
        public ufbx_shader_texture_input* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
