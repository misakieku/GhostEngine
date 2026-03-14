namespace Ghost.Ufbx
{
    public partial struct ufbx_tessellate_surface_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_allocator_opts temp_allocator;

        public ufbx_allocator_opts result_allocator;

        [NativeTypeName("size_t")]
        public nuint span_subdivision_u;

        [NativeTypeName("size_t")]
        public nuint span_subdivision_v;

        [NativeTypeName("_Bool")]
        public bool skip_mesh_parts;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
