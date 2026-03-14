namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_element_list
    {
        public ufbx_element** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
