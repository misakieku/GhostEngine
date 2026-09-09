namespace Ghost.MeshOptimizer
{
    /// <include file='meshopt_VertexFetchStatistics.xml' path='doc/member[@name="meshopt_VertexFetchStatistics"]/*' />
    public partial struct meshopt_VertexFetchStatistics
    {
        /// <include file='meshopt_VertexFetchStatistics.xml' path='doc/member[@name="meshopt_VertexFetchStatistics.bytes_fetched"]/*' />
        [NativeTypeName("unsigned int")]
        public uint bytes_fetched;

        /// <include file='meshopt_VertexFetchStatistics.xml' path='doc/member[@name="meshopt_VertexFetchStatistics.overfetch"]/*' />
        public float overfetch;
    }
}
