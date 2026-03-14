namespace Ghost.Ufbx
{
    public partial struct ufbx_geometry_cache_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_allocator_opts temp_allocator;

        public ufbx_allocator_opts result_allocator;

        public ufbx_open_file_cb open_file_cb;

        public double frames_per_second;

        public ufbx_mirror_axis mirror_axis;

        [NativeTypeName("_Bool")]
        public bool use_scale_factor;

        [NativeTypeName("ufbx_real")]
        public float scale_factor;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
