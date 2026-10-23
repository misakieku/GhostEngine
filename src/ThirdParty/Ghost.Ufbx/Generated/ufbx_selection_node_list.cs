namespace Ghost.Ufbx
{
    /// <include file='ufbx_selection_node_list.xml' path='doc/member[@name="ufbx_selection_node_list"]/*' />
    public unsafe partial struct ufbx_selection_node_list
    {
        /// <include file='ufbx_selection_node_list.xml' path='doc/member[@name="ufbx_selection_node_list.data"]/*' />
        public ufbx_selection_node** data;

        /// <include file='ufbx_selection_node_list.xml' path='doc/member[@name="ufbx_selection_node_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
