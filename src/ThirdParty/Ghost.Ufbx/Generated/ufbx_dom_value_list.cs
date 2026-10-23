namespace Ghost.Ufbx
{
    /// <include file='ufbx_dom_value_list.xml' path='doc/member[@name="ufbx_dom_value_list"]/*' />
    public unsafe partial struct ufbx_dom_value_list
    {
        /// <include file='ufbx_dom_value_list.xml' path='doc/member[@name="ufbx_dom_value_list.data"]/*' />
        public ufbx_dom_value* data;

        /// <include file='ufbx_dom_value_list.xml' path='doc/member[@name="ufbx_dom_value_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
