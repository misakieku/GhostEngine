namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_element_list.xml' path='doc/member[@name="ufbx_baked_element_list"]/*' />
    public unsafe partial struct ufbx_baked_element_list
    {
        /// <include file='ufbx_baked_element_list.xml' path='doc/member[@name="ufbx_baked_element_list.data"]/*' />
        public ufbx_baked_element* data;

        /// <include file='ufbx_baked_element_list.xml' path='doc/member[@name="ufbx_baked_element_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
