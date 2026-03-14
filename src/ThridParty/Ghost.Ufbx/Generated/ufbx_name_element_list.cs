namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_name_element_list
    {
        public ufbx_name_element* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
