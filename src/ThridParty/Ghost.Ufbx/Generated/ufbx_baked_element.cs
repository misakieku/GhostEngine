namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_element.xml' path='doc/member[@name="ufbx_baked_element"]/*' />
    public partial struct ufbx_baked_element
    {
        /// <include file='ufbx_baked_element.xml' path='doc/member[@name="ufbx_baked_element.element_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint element_id;

        /// <include file='ufbx_baked_element.xml' path='doc/member[@name="ufbx_baked_element.props"]/*' />
        public ufbx_baked_prop_list props;
    }
}
