namespace Ghost.MeshOptimizer
{
    public partial struct meshopt_Meshlet
    {
        [NativeTypeName("unsigned int")]
        public uint vertex_offset;

        [NativeTypeName("unsigned int")]
        public uint triangle_offset;

        [NativeTypeName("unsigned int")]
        public uint vertex_count;

        [NativeTypeName("unsigned int")]
        public uint triangle_count;
    }
}
