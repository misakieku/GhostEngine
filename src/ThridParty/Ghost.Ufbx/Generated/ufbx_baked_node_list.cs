namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_baked_node_list
    {
        public ufbx_baked_node* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
