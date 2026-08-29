namespace Ghost.Ufbx
{
    /// <include file='ufbx_constraint_list.xml' path='doc/member[@name="ufbx_constraint_list"]/*' />
    public unsafe partial struct ufbx_constraint_list
    {
        /// <include file='ufbx_constraint_list.xml' path='doc/member[@name="ufbx_constraint_list.data"]/*' />
        public ufbx_constraint** data;

        /// <include file='ufbx_constraint_list.xml' path='doc/member[@name="ufbx_constraint_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
