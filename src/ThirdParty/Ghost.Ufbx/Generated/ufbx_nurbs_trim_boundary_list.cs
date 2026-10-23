namespace Ghost.Ufbx
{
    /// <include file='ufbx_nurbs_trim_boundary_list.xml' path='doc/member[@name="ufbx_nurbs_trim_boundary_list"]/*' />
    public unsafe partial struct ufbx_nurbs_trim_boundary_list
    {
        /// <include file='ufbx_nurbs_trim_boundary_list.xml' path='doc/member[@name="ufbx_nurbs_trim_boundary_list.data"]/*' />
        public ufbx_nurbs_trim_boundary** data;

        /// <include file='ufbx_nurbs_trim_boundary_list.xml' path='doc/member[@name="ufbx_nurbs_trim_boundary_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
