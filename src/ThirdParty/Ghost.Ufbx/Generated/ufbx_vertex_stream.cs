namespace Ghost.Ufbx
{
    /// <include file='ufbx_vertex_stream.xml' path='doc/member[@name="ufbx_vertex_stream"]/*' />
    public unsafe partial struct ufbx_vertex_stream
    {
        /// <include file='ufbx_vertex_stream.xml' path='doc/member[@name="ufbx_vertex_stream.data"]/*' />
        public void* data;

        /// <include file='ufbx_vertex_stream.xml' path='doc/member[@name="ufbx_vertex_stream.vertex_count"]/*' />
        [NativeTypeName("size_t")]
        public nuint vertex_count;

        /// <include file='ufbx_vertex_stream.xml' path='doc/member[@name="ufbx_vertex_stream.vertex_size"]/*' />
        [NativeTypeName("size_t")]
        public nuint vertex_size;
    }
}
