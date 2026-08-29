namespace Ghost.Ufbx
{
    /// <include file='ufbx_warning_list.xml' path='doc/member[@name="ufbx_warning_list"]/*' />
    public unsafe partial struct ufbx_warning_list
    {
        /// <include file='ufbx_warning_list.xml' path='doc/member[@name="ufbx_warning_list.data"]/*' />
        public ufbx_warning* data;

        /// <include file='ufbx_warning_list.xml' path='doc/member[@name="ufbx_warning_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
