namespace Ghost.Ufbx
{
    /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts"]/*' />
    public partial struct ufbx_geometry_cache_opts
    {
        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts.temp_allocator"]/*' />
        public ufbx_allocator_opts temp_allocator;

        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts.result_allocator"]/*' />
        public ufbx_allocator_opts result_allocator;

        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts.open_file_cb"]/*' />
        public ufbx_open_file_cb open_file_cb;

        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts.frames_per_second"]/*' />
        public double frames_per_second;

        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts.mirror_axis"]/*' />
        public ufbx_mirror_axis mirror_axis;

        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts.use_scale_factor"]/*' />
        [NativeTypeName("_Bool")]
        public bool use_scale_factor;

        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts.scale_factor"]/*' />
        [NativeTypeName("ufbx_real")]
        public float scale_factor;

        /// <include file='ufbx_geometry_cache_opts.xml' path='doc/member[@name="ufbx_geometry_cache_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
