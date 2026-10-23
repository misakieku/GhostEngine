namespace Ghost.Ufbx
{
    /// <include file='ufbx_transform_override_list.xml' path='doc/member[@name="ufbx_transform_override_list"]/*' />
    public unsafe partial struct ufbx_transform_override_list
    {
        /// <include file='ufbx_transform_override_list.xml' path='doc/member[@name="ufbx_transform_override_list.data"]/*' />
        public ufbx_transform_override* data;

        /// <include file='ufbx_transform_override_list.xml' path='doc/member[@name="ufbx_transform_override_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
