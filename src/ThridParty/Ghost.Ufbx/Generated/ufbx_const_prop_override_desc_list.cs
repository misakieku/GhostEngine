namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_const_prop_override_desc_list
    {
        [NativeTypeName("const ufbx_prop_override_desc *")]
        public ufbx_prop_override_desc* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
