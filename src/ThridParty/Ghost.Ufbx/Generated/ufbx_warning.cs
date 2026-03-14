namespace Ghost.Ufbx
{
    public partial struct ufbx_warning
    {
        public ufbx_warning_type type;

        public ufbx_string description;

        [NativeTypeName("uint32_t")]
        public uint element_id;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
