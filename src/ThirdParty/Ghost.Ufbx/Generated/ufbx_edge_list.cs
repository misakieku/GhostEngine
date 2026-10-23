namespace Ghost.Ufbx
{
    /// <include file='ufbx_edge_list.xml' path='doc/member[@name="ufbx_edge_list"]/*' />
    public unsafe partial struct ufbx_edge_list
    {
        /// <include file='ufbx_edge_list.xml' path='doc/member[@name="ufbx_edge_list.data"]/*' />
        public ufbx_edge* data;

        /// <include file='ufbx_edge_list.xml' path='doc/member[@name="ufbx_edge_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
