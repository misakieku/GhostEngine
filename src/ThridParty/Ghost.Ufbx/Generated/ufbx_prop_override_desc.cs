namespace Ghost.Ufbx
{
    /// <include file='ufbx_prop_override_desc.xml' path='doc/member[@name="ufbx_prop_override_desc"]/*' />
    public partial struct ufbx_prop_override_desc
    {
        /// <include file='ufbx_prop_override_desc.xml' path='doc/member[@name="ufbx_prop_override_desc.element_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint element_id;

        /// <include file='ufbx_prop_override_desc.xml' path='doc/member[@name="ufbx_prop_override_desc.prop_name"]/*' />
        public ufbx_string prop_name;

        /// <include file='ufbx_prop_override_desc.xml' path='doc/member[@name="ufbx_prop_override_desc.value"]/*' />
        public ufbx_vec4 value;

        /// <include file='ufbx_prop_override_desc.xml' path='doc/member[@name="ufbx_prop_override_desc.value_str"]/*' />
        public ufbx_string value_str;

        /// <include file='ufbx_prop_override_desc.xml' path='doc/member[@name="ufbx_prop_override_desc.value_int"]/*' />
        [NativeTypeName("int64_t")]
        public long value_int;
    }
}
