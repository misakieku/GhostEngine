namespace Ghost.Ufbx
{
    /// <include file='ufbx_geometry_cache_data_opts.xml' path='doc/member[@name="ufbx_geometry_cache_data_opts"]/*' />
    public partial struct ufbx_geometry_cache_data_opts
    {
        /// <include file='ufbx_geometry_cache_data_opts.xml' path='doc/member[@name="ufbx_geometry_cache_data_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_geometry_cache_data_opts.xml' path='doc/member[@name="ufbx_geometry_cache_data_opts.open_file_cb"]/*' />
        public ufbx_open_file_cb open_file_cb;

        /// <include file='ufbx_geometry_cache_data_opts.xml' path='doc/member[@name="ufbx_geometry_cache_data_opts.additive"]/*' />
        [NativeTypeName("_Bool")]
        public bool additive;

        /// <include file='ufbx_geometry_cache_data_opts.xml' path='doc/member[@name="ufbx_geometry_cache_data_opts.use_weight"]/*' />
        [NativeTypeName("_Bool")]
        public bool use_weight;

        /// <include file='ufbx_geometry_cache_data_opts.xml' path='doc/member[@name="ufbx_geometry_cache_data_opts.weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float weight;

        /// <include file='ufbx_geometry_cache_data_opts.xml' path='doc/member[@name="ufbx_geometry_cache_data_opts.ignore_transform"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_transform;

        /// <include file='ufbx_geometry_cache_data_opts.xml' path='doc/member[@name="ufbx_geometry_cache_data_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
