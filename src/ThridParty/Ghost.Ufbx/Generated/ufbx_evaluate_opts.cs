namespace Ghost.Ufbx
{
    /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts"]/*' />
    public partial struct ufbx_evaluate_opts
    {
        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts.temp_allocator"]/*' />
        public ufbx_allocator_opts temp_allocator;

        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts.result_allocator"]/*' />
        public ufbx_allocator_opts result_allocator;

        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts.evaluate_skinning"]/*' />
        [NativeTypeName("_Bool")]
        public bool evaluate_skinning;

        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts.evaluate_caches"]/*' />
        [NativeTypeName("_Bool")]
        public bool evaluate_caches;

        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts.evaluate_flags"]/*' />
        [NativeTypeName("uint32_t")]
        public uint evaluate_flags;

        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts.load_external_files"]/*' />
        [NativeTypeName("_Bool")]
        public bool load_external_files;

        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts.open_file_cb"]/*' />
        public ufbx_open_file_cb open_file_cb;

        /// <include file='ufbx_evaluate_opts.xml' path='doc/member[@name="ufbx_evaluate_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
