namespace Ghost.Ufbx
{
    public partial struct ufbx_texture_file
    {
        [NativeTypeName("uint32_t")]
        public uint index;

        public ufbx_string filename;

        public ufbx_string absolute_filename;

        public ufbx_string relative_filename;

        public ufbx_blob raw_filename;

        public ufbx_blob raw_absolute_filename;

        public ufbx_blob raw_relative_filename;

        public ufbx_blob content;
    }
}
