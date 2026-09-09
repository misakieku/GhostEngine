namespace Ghost.Ufbx
{
    /// <include file='ufbx_element_list.xml' path='doc/member[@name="ufbx_element_list"]/*' />
    public unsafe partial struct ufbx_element_list
    {
        /// <include file='ufbx_element_list.xml' path='doc/member[@name="ufbx_element_list.data"]/*' />
        public ufbx_element** data;

        /// <include file='ufbx_element_list.xml' path='doc/member[@name="ufbx_element_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
