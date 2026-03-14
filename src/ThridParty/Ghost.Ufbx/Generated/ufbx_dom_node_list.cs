namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_dom_node_list
    {
        public ufbx_dom_node** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
