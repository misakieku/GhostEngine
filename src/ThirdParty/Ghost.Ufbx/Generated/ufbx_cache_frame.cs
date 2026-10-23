namespace Ghost.Ufbx
{
    /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame"]/*' />
    public partial struct ufbx_cache_frame
    {
        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.channel"]/*' />
        public ufbx_string channel;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.time"]/*' />
        public double time;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.filename"]/*' />
        public ufbx_string filename;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.file_format"]/*' />
        public ufbx_cache_file_format file_format;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.mirror_axis"]/*' />
        public ufbx_mirror_axis mirror_axis;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.scale_factor"]/*' />
        [NativeTypeName("ufbx_real")]
        public float scale_factor;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.data_format"]/*' />
        public ufbx_cache_data_format data_format;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.data_encoding"]/*' />
        public ufbx_cache_data_encoding data_encoding;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.data_offset"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong data_offset;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.data_count"]/*' />
        [NativeTypeName("uint32_t")]
        public uint data_count;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.data_element_bytes"]/*' />
        [NativeTypeName("uint32_t")]
        public uint data_element_bytes;

        /// <include file='ufbx_cache_frame.xml' path='doc/member[@name="ufbx_cache_frame.data_total_bytes"]/*' />
        [NativeTypeName("uint64_t")]
        public ulong data_total_bytes;
    }
}
