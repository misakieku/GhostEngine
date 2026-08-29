namespace Ghost.Ufbx
{
    /// <include file='ufbx_empty_list.xml' path='doc/member[@name="ufbx_empty_list"]/*' />
    public unsafe partial struct ufbx_empty_list
    {
        /// <include file='ufbx_empty_list.xml' path='doc/member[@name="ufbx_empty_list.data"]/*' />
        public ufbx_empty** data;

        /// <include file='ufbx_empty_list.xml' path='doc/member[@name="ufbx_empty_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
