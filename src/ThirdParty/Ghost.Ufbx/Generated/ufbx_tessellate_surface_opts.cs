namespace Ghost.Ufbx
{
    /// <include file='ufbx_tessellate_surface_opts.xml' path='doc/member[@name="ufbx_tessellate_surface_opts"]/*' />
    public partial struct ufbx_tessellate_surface_opts
    {
        /// <include file='ufbx_tessellate_surface_opts.xml' path='doc/member[@name="ufbx_tessellate_surface_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_tessellate_surface_opts.xml' path='doc/member[@name="ufbx_tessellate_surface_opts.temp_allocator"]/*' />
        public ufbx_allocator_opts temp_allocator;

        /// <include file='ufbx_tessellate_surface_opts.xml' path='doc/member[@name="ufbx_tessellate_surface_opts.result_allocator"]/*' />
        public ufbx_allocator_opts result_allocator;

        /// <include file='ufbx_tessellate_surface_opts.xml' path='doc/member[@name="ufbx_tessellate_surface_opts.span_subdivision_u"]/*' />
        [NativeTypeName("size_t")]
        public nuint span_subdivision_u;

        /// <include file='ufbx_tessellate_surface_opts.xml' path='doc/member[@name="ufbx_tessellate_surface_opts.span_subdivision_v"]/*' />
        [NativeTypeName("size_t")]
        public nuint span_subdivision_v;

        /// <include file='ufbx_tessellate_surface_opts.xml' path='doc/member[@name="ufbx_tessellate_surface_opts.skip_mesh_parts"]/*' />
        [NativeTypeName("_Bool")]
        public bool skip_mesh_parts;

        /// <include file='ufbx_tessellate_surface_opts.xml' path='doc/member[@name="ufbx_tessellate_surface_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
