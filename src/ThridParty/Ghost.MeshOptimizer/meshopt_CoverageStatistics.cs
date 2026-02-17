using Ghost.Zeux.MeshOptimizer;
using System.Runtime.CompilerServices;

namespace Ghost.MeshOptimizer
{
    public partial struct meshopt_CoverageStatistics
    {
        [NativeTypeName("float[3]")]
        public _coverage_e__FixedBuffer coverage;

        public float extent;

        [InlineArray(3)]
        public partial struct _coverage_e__FixedBuffer
        {
            public float e0;
        }
    }
}
