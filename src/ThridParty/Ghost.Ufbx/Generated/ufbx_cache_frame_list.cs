namespace Ghost.Ufbx
{
    /// <include file='ufbx_cache_frame_list.xml' path='doc/member[@name="ufbx_cache_frame_list"]/*' />
    public unsafe partial struct ufbx_cache_frame_list
    {
        /// <include file='ufbx_cache_frame_list.xml' path='doc/member[@name="ufbx_cache_frame_list.data"]/*' />
        public ufbx_cache_frame* data;

        /// <include file='ufbx_cache_frame_list.xml' path='doc/member[@name="ufbx_cache_frame_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
