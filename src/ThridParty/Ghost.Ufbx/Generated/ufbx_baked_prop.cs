namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_prop.xml' path='doc/member[@name="ufbx_baked_prop"]/*' />
    public partial struct ufbx_baked_prop
    {
        /// <include file='ufbx_baked_prop.xml' path='doc/member[@name="ufbx_baked_prop.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_baked_prop.xml' path='doc/member[@name="ufbx_baked_prop.constant_value"]/*' />
        [NativeTypeName("_Bool")]
        public bool constant_value;

        /// <include file='ufbx_baked_prop.xml' path='doc/member[@name="ufbx_baked_prop.keys"]/*' />
        public ufbx_baked_vec3_list keys;
    }
}
