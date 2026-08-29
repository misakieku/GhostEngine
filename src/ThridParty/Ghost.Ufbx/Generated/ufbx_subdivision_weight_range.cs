namespace Ghost.Ufbx
{
    /// <include file='ufbx_subdivision_weight_range.xml' path='doc/member[@name="ufbx_subdivision_weight_range"]/*' />
    public partial struct ufbx_subdivision_weight_range
    {
        /// <include file='ufbx_subdivision_weight_range.xml' path='doc/member[@name="ufbx_subdivision_weight_range.weight_begin"]/*' />
        [NativeTypeName("uint32_t")]
        public uint weight_begin;

        /// <include file='ufbx_subdivision_weight_range.xml' path='doc/member[@name="ufbx_subdivision_weight_range.num_weights"]/*' />
        [NativeTypeName("uint32_t")]
        public uint num_weights;
    }
}
