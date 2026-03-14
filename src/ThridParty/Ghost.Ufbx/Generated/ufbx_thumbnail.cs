namespace Ghost.Ufbx
{
    public partial struct ufbx_thumbnail
    {
        public ufbx_props props;

        [NativeTypeName("uint32_t")]
        public uint width;

        [NativeTypeName("uint32_t")]
        public uint height;

        public ufbx_thumbnail_format format;

        public ufbx_blob data;
    }
}
