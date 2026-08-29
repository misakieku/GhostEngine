namespace Ghost.Ufbx
{
    /// <include file='ufbx_subdivision_weight_range_list.xml' path='doc/member[@name="ufbx_subdivision_weight_range_list"]/*' />
    public unsafe partial struct ufbx_subdivision_weight_range_list
    {
        /// <include file='ufbx_subdivision_weight_range_list.xml' path='doc/member[@name="ufbx_subdivision_weight_range_list.data"]/*' />
        public ufbx_subdivision_weight_range* data;

        /// <include file='ufbx_subdivision_weight_range_list.xml' path='doc/member[@name="ufbx_subdivision_weight_range_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
