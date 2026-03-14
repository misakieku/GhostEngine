namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_baked_element_list
    {
        public ufbx_baked_element* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
