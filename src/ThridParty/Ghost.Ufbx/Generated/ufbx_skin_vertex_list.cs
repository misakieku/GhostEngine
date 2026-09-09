namespace Ghost.Ufbx
{
    /// <include file='ufbx_skin_vertex_list.xml' path='doc/member[@name="ufbx_skin_vertex_list"]/*' />
    public unsafe partial struct ufbx_skin_vertex_list
    {
        /// <include file='ufbx_skin_vertex_list.xml' path='doc/member[@name="ufbx_skin_vertex_list.data"]/*' />
        public ufbx_skin_vertex* data;

        /// <include file='ufbx_skin_vertex_list.xml' path='doc/member[@name="ufbx_skin_vertex_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
