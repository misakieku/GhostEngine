namespace Ghost.Ufbx
{
    /// <include file='ufbx_cache_channel.xml' path='doc/member[@name="ufbx_cache_channel"]/*' />
    public partial struct ufbx_cache_channel
    {
        /// <include file='ufbx_cache_channel.xml' path='doc/member[@name="ufbx_cache_channel.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_cache_channel.xml' path='doc/member[@name="ufbx_cache_channel.interpretation"]/*' />
        public ufbx_cache_interpretation interpretation;

        /// <include file='ufbx_cache_channel.xml' path='doc/member[@name="ufbx_cache_channel.interpretation_name"]/*' />
        public ufbx_string interpretation_name;

        /// <include file='ufbx_cache_channel.xml' path='doc/member[@name="ufbx_cache_channel.frames"]/*' />
        public ufbx_cache_frame_list frames;

        /// <include file='ufbx_cache_channel.xml' path='doc/member[@name="ufbx_cache_channel.mirror_axis"]/*' />
        public ufbx_mirror_axis mirror_axis;

        /// <include file='ufbx_cache_channel.xml' path='doc/member[@name="ufbx_cache_channel.scale_factor"]/*' />
        [NativeTypeName("ufbx_real")]
        public float scale_factor;
    }
}
