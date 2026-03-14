namespace Ghost.Ufbx
{
    public partial struct ufbx_cache_frame
    {
        public ufbx_string channel;

        public double time;

        public ufbx_string filename;

        public ufbx_cache_file_format file_format;

        public ufbx_mirror_axis mirror_axis;

        [NativeTypeName("ufbx_real")]
        public float scale_factor;

        public ufbx_cache_data_format data_format;

        public ufbx_cache_data_encoding data_encoding;

        [NativeTypeName("uint64_t")]
        public ulong data_offset;

        [NativeTypeName("uint32_t")]
        public uint data_count;

        [NativeTypeName("uint32_t")]
        public uint data_element_bytes;

        [NativeTypeName("uint64_t")]
        public ulong data_total_bytes;
    }
}
