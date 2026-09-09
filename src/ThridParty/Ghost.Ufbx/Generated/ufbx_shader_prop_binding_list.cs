namespace Ghost.Ufbx
{
    /// <include file='ufbx_shader_prop_binding_list.xml' path='doc/member[@name="ufbx_shader_prop_binding_list"]/*' />
    public unsafe partial struct ufbx_shader_prop_binding_list
    {
        /// <include file='ufbx_shader_prop_binding_list.xml' path='doc/member[@name="ufbx_shader_prop_binding_list.data"]/*' />
        public ufbx_shader_prop_binding* data;

        /// <include file='ufbx_shader_prop_binding_list.xml' path='doc/member[@name="ufbx_shader_prop_binding_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
