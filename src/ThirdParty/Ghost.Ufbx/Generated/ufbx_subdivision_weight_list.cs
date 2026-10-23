namespace Ghost.Ufbx
{
    /// <include file='ufbx_subdivision_weight_list.xml' path='doc/member[@name="ufbx_subdivision_weight_list"]/*' />
    public unsafe partial struct ufbx_subdivision_weight_list
    {
        /// <include file='ufbx_subdivision_weight_list.xml' path='doc/member[@name="ufbx_subdivision_weight_list.data"]/*' />
        public ufbx_subdivision_weight* data;

        /// <include file='ufbx_subdivision_weight_list.xml' path='doc/member[@name="ufbx_subdivision_weight_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
