namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_name_element
    {
        public ufbx_string name;

        public ufbx_element_type type;

        [NativeTypeName("uint32_t")]
        public uint _internal_key;

        public ufbx_element* element;
    }
}
