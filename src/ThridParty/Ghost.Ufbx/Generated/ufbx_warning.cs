namespace Ghost.Ufbx
{
    /// <include file='ufbx_warning.xml' path='doc/member[@name="ufbx_warning"]/*' />
    public partial struct ufbx_warning
    {
        /// <include file='ufbx_warning.xml' path='doc/member[@name="ufbx_warning.type"]/*' />
        public ufbx_warning_type type;

        /// <include file='ufbx_warning.xml' path='doc/member[@name="ufbx_warning.description"]/*' />
        public ufbx_string description;

        /// <include file='ufbx_warning.xml' path='doc/member[@name="ufbx_warning.element_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint element_id;

        /// <include file='ufbx_warning.xml' path='doc/member[@name="ufbx_warning.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
