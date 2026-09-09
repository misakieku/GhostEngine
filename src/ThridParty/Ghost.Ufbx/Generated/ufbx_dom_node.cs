namespace Ghost.Ufbx
{
    /// <include file='ufbx_dom_node.xml' path='doc/member[@name="ufbx_dom_node"]/*' />
    public partial struct ufbx_dom_node
    {
        /// <include file='ufbx_dom_node.xml' path='doc/member[@name="ufbx_dom_node.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_dom_node.xml' path='doc/member[@name="ufbx_dom_node.children"]/*' />
        public ufbx_dom_node_list children;

        /// <include file='ufbx_dom_node.xml' path='doc/member[@name="ufbx_dom_node.values"]/*' />
        public ufbx_dom_value_list values;
    }
}
