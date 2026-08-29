using System.Runtime.CompilerServices;

namespace Ghost.MeshOptimizer
{
    /// <include file='meshopt_CoverageStatistics.xml' path='doc/member[@name="meshopt_CoverageStatistics"]/*' />
    public partial struct meshopt_CoverageStatistics
    {
        /// <include file='meshopt_CoverageStatistics.xml' path='doc/member[@name="meshopt_CoverageStatistics.coverage"]/*' />
        [NativeTypeName("float[3]")]
        public _coverage_e__FixedBuffer coverage;

        /// <include file='meshopt_CoverageStatistics.xml' path='doc/member[@name="meshopt_CoverageStatistics.extent"]/*' />
        public float extent;

        /// <include file='_coverage_e__FixedBuffer.xml' path='doc/member[@name="_coverage_e__FixedBuffer"]/*' />
        [InlineArray(3)]
        public partial struct _coverage_e__FixedBuffer
        {
            public float e0;
        }
    }
}
