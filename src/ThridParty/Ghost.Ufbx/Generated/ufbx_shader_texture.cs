namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_shader_texture
    {
        public ufbx_shader_texture_type type;

        public ufbx_string shader_name;

        [NativeTypeName("uint64_t")]
        public ulong shader_type_id;

        public ufbx_shader_texture_input_list inputs;

        public ufbx_string shader_source;

        public ufbx_blob raw_shader_source;

        public ufbx_texture* main_texture;

        [NativeTypeName("int64_t")]
        public long main_texture_output_index;

        public ufbx_string prop_prefix;
    }
}
