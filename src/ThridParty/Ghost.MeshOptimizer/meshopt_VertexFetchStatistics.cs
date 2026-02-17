using Ghost.Zeux.MeshOptimizer;

namespace Ghost.MeshOptimizer
{
    public partial struct meshopt_VertexFetchStatistics
    {
        [NativeTypeName("unsigned int")]
        public uint bytes_fetched;

        public float overfetch;
    }
}
