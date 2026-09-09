namespace Ghost.Ufbx
{
    /// <include file='ufbx_anim_prop.xml' path='doc/member[@name="ufbx_anim_prop"]/*' />
    public unsafe partial struct ufbx_anim_prop
    {
        /// <include file='ufbx_anim_prop.xml' path='doc/member[@name="ufbx_anim_prop.element"]/*' />
        public ufbx_element* element;

        /// <include file='ufbx_anim_prop.xml' path='doc/member[@name="ufbx_anim_prop._internal_key"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _internal_key;

        /// <include file='ufbx_anim_prop.xml' path='doc/member[@name="ufbx_anim_prop.prop_name"]/*' />
        public ufbx_string prop_name;

        /// <include file='ufbx_anim_prop.xml' path='doc/member[@name="ufbx_anim_prop.anim_value"]/*' />
        public ufbx_anim_value* anim_value;
    }
}
