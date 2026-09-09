namespace Ghost.Ufbx
{
    /// <include file='ufbx_subdivision_weight.xml' path='doc/member[@name="ufbx_subdivision_weight"]/*' />
    public partial struct ufbx_subdivision_weight
    {
        /// <include file='ufbx_subdivision_weight.xml' path='doc/member[@name="ufbx_subdivision_weight.weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float weight;

        /// <include file='ufbx_subdivision_weight.xml' path='doc/member[@name="ufbx_subdivision_weight.index"]/*' />
        [NativeTypeName("uint32_t")]
        public uint index;
    }
}
