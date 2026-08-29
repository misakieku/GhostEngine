namespace Ghost.Ufbx
{
    /// <include file='ufbx_dom_node_list.xml' path='doc/member[@name="ufbx_dom_node_list"]/*' />
    public unsafe partial struct ufbx_dom_node_list
    {
        /// <include file='ufbx_dom_node_list.xml' path='doc/member[@name="ufbx_dom_node_list.data"]/*' />
        public ufbx_dom_node** data;

        /// <include file='ufbx_dom_node_list.xml' path='doc/member[@name="ufbx_dom_node_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
