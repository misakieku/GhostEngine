namespace Ghost.Ufbx
{
    /// <include file='ufbx_shader_texture_input_list.xml' path='doc/member[@name="ufbx_shader_texture_input_list"]/*' />
    public unsafe partial struct ufbx_shader_texture_input_list
    {
        /// <include file='ufbx_shader_texture_input_list.xml' path='doc/member[@name="ufbx_shader_texture_input_list.data"]/*' />
        public ufbx_shader_texture_input* data;

        /// <include file='ufbx_shader_texture_input_list.xml' path='doc/member[@name="ufbx_shader_texture_input_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
