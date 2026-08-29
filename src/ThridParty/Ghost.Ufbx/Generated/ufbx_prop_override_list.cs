namespace Ghost.Ufbx
{
    /// <include file='ufbx_prop_override_list.xml' path='doc/member[@name="ufbx_prop_override_list"]/*' />
    public unsafe partial struct ufbx_prop_override_list
    {
        /// <include file='ufbx_prop_override_list.xml' path='doc/member[@name="ufbx_prop_override_list.data"]/*' />
        public ufbx_prop_override* data;

        /// <include file='ufbx_prop_override_list.xml' path='doc/member[@name="ufbx_prop_override_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
