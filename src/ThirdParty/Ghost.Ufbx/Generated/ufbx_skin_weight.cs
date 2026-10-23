namespace Ghost.Ufbx
{
    /// <include file='ufbx_skin_weight.xml' path='doc/member[@name="ufbx_skin_weight"]/*' />
    public partial struct ufbx_skin_weight
    {
        /// <include file='ufbx_skin_weight.xml' path='doc/member[@name="ufbx_skin_weight.cluster_index"]/*' />
        [NativeTypeName("uint32_t")]
        public uint cluster_index;

        /// <include file='ufbx_skin_weight.xml' path='doc/member[@name="ufbx_skin_weight.weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float weight;
    }
}
