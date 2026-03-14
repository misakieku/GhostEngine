namespace Ghost.Ufbx
{
    public partial struct ufbx_open_file_info
    {
        [NativeTypeName("ufbx_open_file_context")]
        public nuint context;

        public ufbx_open_file_type type;

        public ufbx_blob original_filename;
    }
}
