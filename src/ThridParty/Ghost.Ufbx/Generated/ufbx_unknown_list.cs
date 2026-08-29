namespace Ghost.Ufbx
{
    /// <include file='ufbx_unknown_list.xml' path='doc/member[@name="ufbx_unknown_list"]/*' />
    public unsafe partial struct ufbx_unknown_list
    {
        /// <include file='ufbx_unknown_list.xml' path='doc/member[@name="ufbx_unknown_list.data"]/*' />
        public ufbx_unknown** data;

        /// <include file='ufbx_unknown_list.xml' path='doc/member[@name="ufbx_unknown_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
