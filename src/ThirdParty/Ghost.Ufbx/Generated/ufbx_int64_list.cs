namespace Ghost.Ufbx
{
    /// <include file='ufbx_int64_list.xml' path='doc/member[@name="ufbx_int64_list"]/*' />
    public unsafe partial struct ufbx_int64_list
    {
        /// <include file='ufbx_int64_list.xml' path='doc/member[@name="ufbx_int64_list.data"]/*' />
        [NativeTypeName("int64_t *")]
        public long* data;

        /// <include file='ufbx_int64_list.xml' path='doc/member[@name="ufbx_int64_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
