namespace Ghost.MeshOptimizer
{
    /// <include file='meshopt_VertexCacheStatistics.xml' path='doc/member[@name="meshopt_VertexCacheStatistics"]/*' />
    public partial struct meshopt_VertexCacheStatistics
    {
        /// <include file='meshopt_VertexCacheStatistics.xml' path='doc/member[@name="meshopt_VertexCacheStatistics.vertices_transformed"]/*' />
        [NativeTypeName("unsigned int")]
        public uint vertices_transformed;

        /// <include file='meshopt_VertexCacheStatistics.xml' path='doc/member[@name="meshopt_VertexCacheStatistics.warps_executed"]/*' />
        [NativeTypeName("unsigned int")]
        public uint warps_executed;

        /// <include file='meshopt_VertexCacheStatistics.xml' path='doc/member[@name="meshopt_VertexCacheStatistics.acmr"]/*' />
        public float acmr;

        /// <include file='meshopt_VertexCacheStatistics.xml' path='doc/member[@name="meshopt_VertexCacheStatistics.atvr"]/*' />
        public float atvr;
    }
}
