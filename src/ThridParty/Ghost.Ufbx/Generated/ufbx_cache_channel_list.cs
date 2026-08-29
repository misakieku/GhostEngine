namespace Ghost.Ufbx
{
    /// <include file='ufbx_cache_channel_list.xml' path='doc/member[@name="ufbx_cache_channel_list"]/*' />
    public unsafe partial struct ufbx_cache_channel_list
    {
        /// <include file='ufbx_cache_channel_list.xml' path='doc/member[@name="ufbx_cache_channel_list.data"]/*' />
        public ufbx_cache_channel* data;

        /// <include file='ufbx_cache_channel_list.xml' path='doc/member[@name="ufbx_cache_channel_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
