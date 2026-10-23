namespace Ghost.MeshOptimizer
{
    /// <include file='meshopt_Stream.xml' path='doc/member[@name="meshopt_Stream"]/*' />
    public unsafe partial struct meshopt_Stream
    {
        /// <include file='meshopt_Stream.xml' path='doc/member[@name="meshopt_Stream.data"]/*' />
        [NativeTypeName("const void *")]
        public void* data;

        /// <include file='meshopt_Stream.xml' path='doc/member[@name="meshopt_Stream.size"]/*' />
        [NativeTypeName("size_t")]
        public nuint size;

        /// <include file='meshopt_Stream.xml' path='doc/member[@name="meshopt_Stream.stride"]/*' />
        [NativeTypeName("size_t")]
        public nuint stride;
    }
}
