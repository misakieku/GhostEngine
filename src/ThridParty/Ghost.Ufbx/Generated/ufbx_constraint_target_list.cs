namespace Ghost.Ufbx
{
    /// <include file='ufbx_constraint_target_list.xml' path='doc/member[@name="ufbx_constraint_target_list"]/*' />
    public unsafe partial struct ufbx_constraint_target_list
    {
        /// <include file='ufbx_constraint_target_list.xml' path='doc/member[@name="ufbx_constraint_target_list.data"]/*' />
        public ufbx_constraint_target* data;

        /// <include file='ufbx_constraint_target_list.xml' path='doc/member[@name="ufbx_constraint_target_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
