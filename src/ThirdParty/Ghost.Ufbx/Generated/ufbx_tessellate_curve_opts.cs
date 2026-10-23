namespace Ghost.Ufbx
{
    /// <include file='ufbx_tessellate_curve_opts.xml' path='doc/member[@name="ufbx_tessellate_curve_opts"]/*' />
    public partial struct ufbx_tessellate_curve_opts
    {
        /// <include file='ufbx_tessellate_curve_opts.xml' path='doc/member[@name="ufbx_tessellate_curve_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_tessellate_curve_opts.xml' path='doc/member[@name="ufbx_tessellate_curve_opts.temp_allocator"]/*' />
        public ufbx_allocator_opts temp_allocator;

        /// <include file='ufbx_tessellate_curve_opts.xml' path='doc/member[@name="ufbx_tessellate_curve_opts.result_allocator"]/*' />
        public ufbx_allocator_opts result_allocator;

        /// <include file='ufbx_tessellate_curve_opts.xml' path='doc/member[@name="ufbx_tessellate_curve_opts.span_subdivision"]/*' />
        [NativeTypeName("size_t")]
        public nuint span_subdivision;

        /// <include file='ufbx_tessellate_curve_opts.xml' path='doc/member[@name="ufbx_tessellate_curve_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
