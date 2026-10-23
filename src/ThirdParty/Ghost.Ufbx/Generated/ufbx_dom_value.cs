namespace Ghost.Ufbx
{
    /// <include file='ufbx_dom_value.xml' path='doc/member[@name="ufbx_dom_value"]/*' />
    public partial struct ufbx_dom_value
    {
        /// <include file='ufbx_dom_value.xml' path='doc/member[@name="ufbx_dom_value.type"]/*' />
        public ufbx_dom_value_type type;

        /// <include file='ufbx_dom_value.xml' path='doc/member[@name="ufbx_dom_value.value_str"]/*' />
        public ufbx_string value_str;

        /// <include file='ufbx_dom_value.xml' path='doc/member[@name="ufbx_dom_value.value_blob"]/*' />
        public ufbx_blob value_blob;

        /// <include file='ufbx_dom_value.xml' path='doc/member[@name="ufbx_dom_value.value_int"]/*' />
        [NativeTypeName("int64_t")]
        public long value_int;

        /// <include file='ufbx_dom_value.xml' path='doc/member[@name="ufbx_dom_value.value_float"]/*' />
        public double value_float;
    }
}
