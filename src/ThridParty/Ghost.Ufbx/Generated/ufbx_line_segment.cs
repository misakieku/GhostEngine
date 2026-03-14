namespace Ghost.Ufbx
{
    public partial struct ufbx_line_segment
    {
        [NativeTypeName("uint32_t")]
        public uint index_begin;

        [NativeTypeName("uint32_t")]
        public uint num_indices;
    }
}
