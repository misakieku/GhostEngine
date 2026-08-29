namespace Ghost.Ufbx
{
    /// <include file='ufbx_cache_file_list.xml' path='doc/member[@name="ufbx_cache_file_list"]/*' />
    public unsafe partial struct ufbx_cache_file_list
    {
        /// <include file='ufbx_cache_file_list.xml' path='doc/member[@name="ufbx_cache_file_list.data"]/*' />
        public ufbx_cache_file** data;

        /// <include file='ufbx_cache_file_list.xml' path='doc/member[@name="ufbx_cache_file_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
