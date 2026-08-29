namespace Ghost.Ufbx
{
    /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture"]/*' />
    public unsafe partial struct ufbx_shader_texture
    {
        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.type"]/*' />
        public ufbx_shader_texture_type type;

        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.shader_name"]/*' />
        public ufbx_string shader_name;

        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.shader_type_id"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong shader_type_id;

        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.inputs"]/*' />
        public ufbx_shader_texture_input_list inputs;

        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.shader_source"]/*' />
        public ufbx_string shader_source;

        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.raw_shader_source"]/*' />
        public ufbx_blob raw_shader_source;

        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.main_texture"]/*' />
        public ufbx_texture* main_texture;

        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.main_texture_output_index"]/*' />
        [NativeTypeName("int64_t")]
        public long main_texture_output_index;

        /// <include file='ufbx_shader_texture.xml' path='doc/member[@name="ufbx_shader_texture.prop_prefix"]/*' />
        public ufbx_string prop_prefix;
    }
}
