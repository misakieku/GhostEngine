namespace Ghost.Ufbx
{
    /// <include file='ufbx_shader_list.xml' path='doc/member[@name="ufbx_shader_list"]/*' />
    public unsafe partial struct ufbx_shader_list
    {
        /// <include file='ufbx_shader_list.xml' path='doc/member[@name="ufbx_shader_list.data"]/*' />
        public ufbx_shader** data;

        /// <include file='ufbx_shader_list.xml' path='doc/member[@name="ufbx_shader_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
