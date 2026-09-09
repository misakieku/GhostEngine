namespace Ghost.Ufbx
{
    /// <include file='ufbx_geometry_cache.xml' path='doc/member[@name="ufbx_geometry_cache"]/*' />
    public partial struct ufbx_geometry_cache
    {
        /// <include file='ufbx_geometry_cache.xml' path='doc/member[@name="ufbx_geometry_cache.root_filename"]/*' />
        public ufbx_string root_filename;

        /// <include file='ufbx_geometry_cache.xml' path='doc/member[@name="ufbx_geometry_cache.channels"]/*' />
        public ufbx_cache_channel_list channels;

        /// <include file='ufbx_geometry_cache.xml' path='doc/member[@name="ufbx_geometry_cache.frames"]/*' />
        public ufbx_cache_frame_list frames;

        /// <include file='ufbx_geometry_cache.xml' path='doc/member[@name="ufbx_geometry_cache.extra_info"]/*' />
        public ufbx_string_list extra_info;
    }
}
