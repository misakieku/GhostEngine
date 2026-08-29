namespace Ghost.Ufbx
{
    /// <include file='ufbx_prop_override.xml' path='doc/member[@name="ufbx_prop_override"]/*' />
    public partial struct ufbx_prop_override
    {
        /// <include file='ufbx_prop_override.xml' path='doc/member[@name="ufbx_prop_override.element_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint element_id;

        /// <include file='ufbx_prop_override.xml' path='doc/member[@name="ufbx_prop_override._internal_key"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _internal_key;

        /// <include file='ufbx_prop_override.xml' path='doc/member[@name="ufbx_prop_override.prop_name"]/*' />
        public ufbx_string prop_name;

        /// <include file='ufbx_prop_override.xml' path='doc/member[@name="ufbx_prop_override.value"]/*' />
        public ufbx_vec4 value;

        /// <include file='ufbx_prop_override.xml' path='doc/member[@name="ufbx_prop_override.value_str"]/*' />
        public ufbx_string value_str;

        /// <include file='ufbx_prop_override.xml' path='doc/member[@name="ufbx_prop_override.value_int"]/*' />
        [NativeTypeName("int64_t")]
        public long value_int;
    }
}
