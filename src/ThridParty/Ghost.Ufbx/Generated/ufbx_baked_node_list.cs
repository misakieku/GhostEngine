namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_node_list.xml' path='doc/member[@name="ufbx_baked_node_list"]/*' />
    public unsafe partial struct ufbx_baked_node_list
    {
        /// <include file='ufbx_baked_node_list.xml' path='doc/member[@name="ufbx_baked_node_list.data"]/*' />
        public ufbx_baked_node* data;

        /// <include file='ufbx_baked_node_list.xml' path='doc/member[@name="ufbx_baked_node_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
