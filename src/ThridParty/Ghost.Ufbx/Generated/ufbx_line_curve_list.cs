namespace Ghost.Ufbx
{
    /// <include file='ufbx_line_curve_list.xml' path='doc/member[@name="ufbx_line_curve_list"]/*' />
    public unsafe partial struct ufbx_line_curve_list
    {
        /// <include file='ufbx_line_curve_list.xml' path='doc/member[@name="ufbx_line_curve_list.data"]/*' />
        public ufbx_line_curve** data;

        /// <include file='ufbx_line_curve_list.xml' path='doc/member[@name="ufbx_line_curve_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
