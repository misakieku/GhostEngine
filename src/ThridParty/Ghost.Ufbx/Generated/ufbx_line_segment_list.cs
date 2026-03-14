namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_line_segment_list
    {
        public ufbx_line_segment* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
