namespace Ghost.Ufbx
{
    /// <include file='ufbx_audio_clip_list.xml' path='doc/member[@name="ufbx_audio_clip_list"]/*' />
    public unsafe partial struct ufbx_audio_clip_list
    {
        /// <include file='ufbx_audio_clip_list.xml' path='doc/member[@name="ufbx_audio_clip_list.data"]/*' />
        public ufbx_audio_clip** data;

        /// <include file='ufbx_audio_clip_list.xml' path='doc/member[@name="ufbx_audio_clip_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
