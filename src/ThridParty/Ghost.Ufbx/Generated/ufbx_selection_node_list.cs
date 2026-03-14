namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_selection_node_list
    {
        public ufbx_selection_node** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
