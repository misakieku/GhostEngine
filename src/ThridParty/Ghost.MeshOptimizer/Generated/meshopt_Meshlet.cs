namespace Ghost.MeshOptimizer
{
    /// <include file='meshopt_Meshlet.xml' path='doc/member[@name="meshopt_Meshlet"]/*' />
    public partial struct meshopt_Meshlet
    {
        /// <include file='meshopt_Meshlet.xml' path='doc/member[@name="meshopt_Meshlet.vertex_offset"]/*' />
        [NativeTypeName("unsigned int")]
        public uint vertex_offset;

        /// <include file='meshopt_Meshlet.xml' path='doc/member[@name="meshopt_Meshlet.triangle_offset"]/*' />
        [NativeTypeName("unsigned int")]
        public uint triangle_offset;

        /// <include file='meshopt_Meshlet.xml' path='doc/member[@name="meshopt_Meshlet.vertex_count"]/*' />
        [NativeTypeName("unsigned int")]
        public uint vertex_count;

        /// <include file='meshopt_Meshlet.xml' path='doc/member[@name="meshopt_Meshlet.triangle_count"]/*' />
        [NativeTypeName("unsigned int")]
        public uint triangle_count;
    }
}
