namespace Ghost.Ufbx
{
    /// <include file='ufbx_topo_edge.xml' path='doc/member[@name="ufbx_topo_edge"]/*' />
    public partial struct ufbx_topo_edge
    {
        /// <include file='ufbx_topo_edge.xml' path='doc/member[@name="ufbx_topo_edge.index"]/*' />
        [NativeTypeName("uint32_t")]
        public uint index;

        /// <include file='ufbx_topo_edge.xml' path='doc/member[@name="ufbx_topo_edge.next"]/*' />
        [NativeTypeName("uint32_t")]
        public uint next;

        /// <include file='ufbx_topo_edge.xml' path='doc/member[@name="ufbx_topo_edge.prev"]/*' />
        [NativeTypeName("uint32_t")]
        public uint prev;

        /// <include file='ufbx_topo_edge.xml' path='doc/member[@name="ufbx_topo_edge.twin"]/*' />
        [NativeTypeName("uint32_t")]
        public uint twin;

        /// <include file='ufbx_topo_edge.xml' path='doc/member[@name="ufbx_topo_edge.face"]/*' />
        [NativeTypeName("uint32_t")]
        public uint face;

        /// <include file='ufbx_topo_edge.xml' path='doc/member[@name="ufbx_topo_edge.edge"]/*' />
        [NativeTypeName("uint32_t")]
        public uint edge;

        /// <include file='ufbx_topo_edge.xml' path='doc/member[@name="ufbx_topo_edge.flags"]/*' />
        public ufbx_topo_flags flags;
    }
}
