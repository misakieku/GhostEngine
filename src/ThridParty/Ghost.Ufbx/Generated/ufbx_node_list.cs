namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_node_list
    {
        public ufbx_node** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
