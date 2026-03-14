namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_skin_vertex_list
    {
        public ufbx_skin_vertex* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
