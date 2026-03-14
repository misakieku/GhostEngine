namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_selection_set_list
    {
        public ufbx_selection_set** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
