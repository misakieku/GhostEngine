namespace Ghost.Ufbx
{
    /// <include file='ufbx_string_list.xml' path='doc/member[@name="ufbx_string_list"]/*' />
    public unsafe partial struct ufbx_string_list
    {
        /// <include file='ufbx_string_list.xml' path='doc/member[@name="ufbx_string_list.data"]/*' />
        public ufbx_string* data;

        /// <include file='ufbx_string_list.xml' path='doc/member[@name="ufbx_string_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
