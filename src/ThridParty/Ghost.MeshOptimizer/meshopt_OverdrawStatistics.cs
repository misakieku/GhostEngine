using Ghost.Zeux.MeshOptimizer;

namespace Ghost.MeshOptimizer
{
    public partial struct meshopt_OverdrawStatistics
    {
        [NativeTypeName("unsigned int")]
        public uint pixels_covered;

        [NativeTypeName("unsigned int")]
        public uint pixels_shaded;

        public float overdraw;
    }
}
