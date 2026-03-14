namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_element
    {
        public ufbx_string name;

        public ufbx_props props;

        [NativeTypeName("uint32_t")]
        public uint element_id;

        [NativeTypeName("uint32_t")]
        public uint typed_id;

        public ufbx_node_list instances;

        public ufbx_element_type type;

        public ufbx_connection_list connections_src;

        public ufbx_connection_list connections_dst;

        public ufbx_dom_node* dom_node;

        public ufbx_scene* scene;
    }
}
