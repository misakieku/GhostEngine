namespace Ghost.Ufbx
{
    public partial struct ufbx_topo_edge
    {
        [NativeTypeName("uint32_t")]
        public uint index;

        [NativeTypeName("uint32_t")]
        public uint next;

        [NativeTypeName("uint32_t")]
        public uint prev;

        [NativeTypeName("uint32_t")]
        public uint twin;

        [NativeTypeName("uint32_t")]
        public uint face;

        [NativeTypeName("uint32_t")]
        public uint edge;

        public ufbx_topo_flags flags;
    }
}
