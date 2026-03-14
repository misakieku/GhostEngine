namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_vertex_stream
    {
        public void* data;

        [NativeTypeName("size_t")]
        public nuint vertex_count;

        [NativeTypeName("size_t")]
        public nuint vertex_size;
    }
}
