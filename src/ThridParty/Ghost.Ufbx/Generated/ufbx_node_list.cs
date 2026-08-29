namespace Ghost.Ufbx
{
    /// <include file='ufbx_node_list.xml' path='doc/member[@name="ufbx_node_list"]/*' />
    public unsafe partial struct ufbx_node_list
    {
        /// <include file='ufbx_node_list.xml' path='doc/member[@name="ufbx_node_list.data"]/*' />
        public ufbx_node** data;

        /// <include file='ufbx_node_list.xml' path='doc/member[@name="ufbx_node_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
