namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_edge_list
    {
        public ufbx_edge* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
