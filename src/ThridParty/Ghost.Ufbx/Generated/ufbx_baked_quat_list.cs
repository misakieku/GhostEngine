namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_quat_list.xml' path='doc/member[@name="ufbx_baked_quat_list"]/*' />
    public unsafe partial struct ufbx_baked_quat_list
    {
        /// <include file='ufbx_baked_quat_list.xml' path='doc/member[@name="ufbx_baked_quat_list.data"]/*' />
        public ufbx_baked_quat* data;

        /// <include file='ufbx_baked_quat_list.xml' path='doc/member[@name="ufbx_baked_quat_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
