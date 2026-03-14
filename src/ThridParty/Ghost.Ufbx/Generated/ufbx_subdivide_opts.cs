namespace Ghost.Ufbx
{
    public partial struct ufbx_subdivide_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_allocator_opts temp_allocator;

        public ufbx_allocator_opts result_allocator;

        public ufbx_subdivision_boundary boundary;

        public ufbx_subdivision_boundary uv_boundary;

        [NativeTypeName("_Bool")]
        public bool ignore_normals;

        [NativeTypeName("_Bool")]
        public bool interpolate_normals;

        [NativeTypeName("_Bool")]
        public bool interpolate_tangents;

        [NativeTypeName("_Bool")]
        public bool evaluate_source_vertices;

        [NativeTypeName("size_t")]
        public nuint max_source_vertices;

        [NativeTypeName("_Bool")]
        public bool evaluate_skin_weights;

        [NativeTypeName("size_t")]
        public nuint max_skin_weights;

        [NativeTypeName("size_t")]
        public nuint skin_deformer_index;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
