namespace Ghost.Ufbx
{
    /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element"]/*' />
    public unsafe partial struct ufbx_element
    {
        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.props"]/*' />
        public ufbx_props props;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.element_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint element_id;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.typed_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint typed_id;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.instances"]/*' />
        public ufbx_node_list instances;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.type"]/*' />
        public ufbx_element_type type;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.connections_src"]/*' />
        public ufbx_connection_list connections_src;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.connections_dst"]/*' />
        public ufbx_connection_list connections_dst;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.dom_node"]/*' />
        public ufbx_dom_node* dom_node;

        /// <include file='ufbx_element.xml' path='doc/member[@name="ufbx_element.scene"]/*' />
        public ufbx_scene* scene;
    }
}
