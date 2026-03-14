namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_dom_value_list
    {
        public ufbx_dom_value* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
