namespace Ghost.Ufbx
{
    /// <include file='ufbx_line_segment.xml' path='doc/member[@name="ufbx_line_segment"]/*' />
    public partial struct ufbx_line_segment
    {
        /// <include file='ufbx_line_segment.xml' path='doc/member[@name="ufbx_line_segment.index_begin"]/*' />
        [NativeTypeName("uint32_t")]
        public uint index_begin;

        /// <include file='ufbx_line_segment.xml' path='doc/member[@name="ufbx_line_segment.num_indices"]/*' />
        [NativeTypeName("uint32_t")]
        public uint num_indices;
    }
}
