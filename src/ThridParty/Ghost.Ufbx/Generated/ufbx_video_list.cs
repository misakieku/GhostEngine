namespace Ghost.Ufbx
{
    /// <include file='ufbx_video_list.xml' path='doc/member[@name="ufbx_video_list"]/*' />
    public unsafe partial struct ufbx_video_list
    {
        /// <include file='ufbx_video_list.xml' path='doc/member[@name="ufbx_video_list.data"]/*' />
        public ufbx_video** data;

        /// <include file='ufbx_video_list.xml' path='doc/member[@name="ufbx_video_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
