namespace Ghost.Ufbx
{
    /// <include file='ufbx_anim_value_list.xml' path='doc/member[@name="ufbx_anim_value_list"]/*' />
    public unsafe partial struct ufbx_anim_value_list
    {
        /// <include file='ufbx_anim_value_list.xml' path='doc/member[@name="ufbx_anim_value_list.data"]/*' />
        public ufbx_anim_value** data;

        /// <include file='ufbx_anim_value_list.xml' path='doc/member[@name="ufbx_anim_value_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
