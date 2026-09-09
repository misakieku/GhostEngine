namespace Ghost.Ufbx
{
    /// <include file='ufbx_skin_vertex.xml' path='doc/member[@name="ufbx_skin_vertex"]/*' />
    public partial struct ufbx_skin_vertex
    {
        /// <include file='ufbx_skin_vertex.xml' path='doc/member[@name="ufbx_skin_vertex.weight_begin"]/*' />
        [NativeTypeName("uint32_t")]
        public uint weight_begin;

        /// <include file='ufbx_skin_vertex.xml' path='doc/member[@name="ufbx_skin_vertex.num_weights"]/*' />
        [NativeTypeName("uint32_t")]
        public uint num_weights;

        /// <include file='ufbx_skin_vertex.xml' path='doc/member[@name="ufbx_skin_vertex.dq_weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float dq_weight;
    }
}
