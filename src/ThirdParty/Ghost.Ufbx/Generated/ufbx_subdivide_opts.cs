namespace Ghost.Ufbx
{
    /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts"]/*' />
    public partial struct ufbx_subdivide_opts
    {
        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.temp_allocator"]/*' />
        public ufbx_allocator_opts temp_allocator;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.result_allocator"]/*' />
        public ufbx_allocator_opts result_allocator;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.boundary"]/*' />
        public ufbx_subdivision_boundary boundary;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.uv_boundary"]/*' />
        public ufbx_subdivision_boundary uv_boundary;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.ignore_normals"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_normals;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.interpolate_normals"]/*' />
        [NativeTypeName("_Bool")]
        public bool interpolate_normals;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.interpolate_tangents"]/*' />
        [NativeTypeName("_Bool")]
        public bool interpolate_tangents;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.evaluate_source_vertices"]/*' />
        [NativeTypeName("_Bool")]
        public bool evaluate_source_vertices;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.max_source_vertices"]/*' />
        [NativeTypeName("size_t")]
        public nuint max_source_vertices;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.evaluate_skin_weights"]/*' />
        [NativeTypeName("_Bool")]
        public bool evaluate_skin_weights;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.max_skin_weights"]/*' />
        [NativeTypeName("size_t")]
        public nuint max_skin_weights;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts.skin_deformer_index"]/*' />
        [NativeTypeName("size_t")]
        public nuint skin_deformer_index;

        /// <include file='ufbx_subdivide_opts.xml' path='doc/member[@name="ufbx_subdivide_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
