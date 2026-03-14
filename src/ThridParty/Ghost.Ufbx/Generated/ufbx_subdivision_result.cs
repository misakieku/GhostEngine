namespace Ghost.Ufbx
{
    public partial struct ufbx_subdivision_result
    {
        [NativeTypeName("size_t")]
        public nuint result_memory_used;

        [NativeTypeName("size_t")]
        public nuint temp_memory_used;

        [NativeTypeName("size_t")]
        public nuint result_allocs;

        [NativeTypeName("size_t")]
        public nuint temp_allocs;

        public ufbx_subdivision_weight_range_list source_vertex_ranges;

        public ufbx_subdivision_weight_list source_vertex_weights;

        public ufbx_subdivision_weight_range_list skin_cluster_ranges;

        public ufbx_subdivision_weight_list skin_cluster_weights;
    }
}
