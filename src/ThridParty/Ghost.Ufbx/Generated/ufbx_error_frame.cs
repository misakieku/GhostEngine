namespace Ghost.Ufbx
{
    public partial struct ufbx_error_frame
    {
        [NativeTypeName("uint32_t")]
        public uint source_line;

        public ufbx_string function;

        public ufbx_string description;
    }
}
