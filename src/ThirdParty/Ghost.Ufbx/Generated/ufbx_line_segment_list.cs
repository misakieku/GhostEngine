namespace Ghost.Ufbx
{
    /// <include file='ufbx_line_segment_list.xml' path='doc/member[@name="ufbx_line_segment_list"]/*' />
    public unsafe partial struct ufbx_line_segment_list
    {
        /// <include file='ufbx_line_segment_list.xml' path='doc/member[@name="ufbx_line_segment_list.data"]/*' />
        public ufbx_line_segment* data;

        /// <include file='ufbx_line_segment_list.xml' path='doc/member[@name="ufbx_line_segment_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
