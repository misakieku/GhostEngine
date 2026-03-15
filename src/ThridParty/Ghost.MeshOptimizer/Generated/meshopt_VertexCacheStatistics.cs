namespace Ghost.MeshOptimizer
{
    public partial struct meshopt_VertexCacheStatistics
    {
        [NativeTypeName("unsigned int")]
        public uint vertices_transformed;

        [NativeTypeName("unsigned int")]
        public uint warps_executed;

        public float acmr;

        public float atvr;
    }
}
