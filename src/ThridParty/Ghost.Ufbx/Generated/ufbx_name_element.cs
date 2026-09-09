namespace Ghost.Ufbx
{
    /// <include file='ufbx_name_element.xml' path='doc/member[@name="ufbx_name_element"]/*' />
    public unsafe partial struct ufbx_name_element
    {
        /// <include file='ufbx_name_element.xml' path='doc/member[@name="ufbx_name_element.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_name_element.xml' path='doc/member[@name="ufbx_name_element.type"]/*' />
        public ufbx_element_type type;

        /// <include file='ufbx_name_element.xml' path='doc/member[@name="ufbx_name_element._internal_key"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _internal_key;

        /// <include file='ufbx_name_element.xml' path='doc/member[@name="ufbx_name_element.element"]/*' />
        public ufbx_element* element;
    }
}
