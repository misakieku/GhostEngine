namespace Ghost.MeshOptimizer
{
    public unsafe partial struct meshopt_Stream
    {
        [NativeTypeName("const void *")]
        public void* data;

        [NativeTypeName("size_t")]
        public nuint size;

        [NativeTypeName("size_t")]
        public nuint stride;
    }
}
