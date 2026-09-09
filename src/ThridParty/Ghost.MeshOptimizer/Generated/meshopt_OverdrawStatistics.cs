namespace Ghost.MeshOptimizer
{
    /// <include file='meshopt_OverdrawStatistics.xml' path='doc/member[@name="meshopt_OverdrawStatistics"]/*' />
    public partial struct meshopt_OverdrawStatistics
    {
        /// <include file='meshopt_OverdrawStatistics.xml' path='doc/member[@name="meshopt_OverdrawStatistics.pixels_covered"]/*' />
        [NativeTypeName("unsigned int")]
        public uint pixels_covered;

        /// <include file='meshopt_OverdrawStatistics.xml' path='doc/member[@name="meshopt_OverdrawStatistics.pixels_shaded"]/*' />
        [NativeTypeName("unsigned int")]
        public uint pixels_shaded;

        /// <include file='meshopt_OverdrawStatistics.xml' path='doc/member[@name="meshopt_OverdrawStatistics.overdraw"]/*' />
        public float overdraw;
    }
}
