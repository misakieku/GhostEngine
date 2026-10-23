namespace Ghost.Ufbx
{
    /// <include file='ufbx_nurbs_curve_list.xml' path='doc/member[@name="ufbx_nurbs_curve_list"]/*' />
    public unsafe partial struct ufbx_nurbs_curve_list
    {
        /// <include file='ufbx_nurbs_curve_list.xml' path='doc/member[@name="ufbx_nurbs_curve_list.data"]/*' />
        public ufbx_nurbs_curve** data;

        /// <include file='ufbx_nurbs_curve_list.xml' path='doc/member[@name="ufbx_nurbs_curve_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
