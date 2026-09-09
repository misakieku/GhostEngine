namespace Ghost.Ufbx
{
    /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result"]/*' />
    public partial struct ufbx_subdivision_result
    {
        /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result.result_memory_used"]/*' />
        [NativeTypeName("size_t")]
        public nuint result_memory_used;

        /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result.temp_memory_used"]/*' />
        [NativeTypeName("size_t")]
        public nuint temp_memory_used;

        /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result.result_allocs"]/*' />
        [NativeTypeName("size_t")]
        public nuint result_allocs;

        /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result.temp_allocs"]/*' />
        [NativeTypeName("size_t")]
        public nuint temp_allocs;

        /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result.source_vertex_ranges"]/*' />
        public ufbx_subdivision_weight_range_list source_vertex_ranges;

        /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result.source_vertex_weights"]/*' />
        public ufbx_subdivision_weight_list source_vertex_weights;

        /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result.skin_cluster_ranges"]/*' />
        public ufbx_subdivision_weight_range_list skin_cluster_ranges;

        /// <include file='ufbx_subdivision_result.xml' path='doc/member[@name="ufbx_subdivision_result.skin_cluster_weights"]/*' />
        public ufbx_subdivision_weight_list skin_cluster_weights;
    }
}
