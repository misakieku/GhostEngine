namespace Ghost.Ufbx
{
    /// <include file='ufbx_const_prop_override_desc_list.xml' path='doc/member[@name="ufbx_const_prop_override_desc_list"]/*' />
    public unsafe partial struct ufbx_const_prop_override_desc_list
    {
        /// <include file='ufbx_const_prop_override_desc_list.xml' path='doc/member[@name="ufbx_const_prop_override_desc_list.data"]/*' />
        [NativeTypeName("const ufbx_prop_override_desc *")]
        public ufbx_prop_override_desc* data;

        /// <include file='ufbx_const_prop_override_desc_list.xml' path='doc/member[@name="ufbx_const_prop_override_desc_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
