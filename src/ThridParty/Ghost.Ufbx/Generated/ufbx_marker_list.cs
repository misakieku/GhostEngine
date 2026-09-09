namespace Ghost.Ufbx
{
    /// <include file='ufbx_marker_list.xml' path='doc/member[@name="ufbx_marker_list"]/*' />
    public unsafe partial struct ufbx_marker_list
    {
        /// <include file='ufbx_marker_list.xml' path='doc/member[@name="ufbx_marker_list.data"]/*' />
        public ufbx_marker** data;

        /// <include file='ufbx_marker_list.xml' path='doc/member[@name="ufbx_marker_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
