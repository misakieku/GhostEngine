namespace Ghost.Ufbx
{
    /// <include file='ufbx_nurbs_surface_list.xml' path='doc/member[@name="ufbx_nurbs_surface_list"]/*' />
    public unsafe partial struct ufbx_nurbs_surface_list
    {
        /// <include file='ufbx_nurbs_surface_list.xml' path='doc/member[@name="ufbx_nurbs_surface_list.data"]/*' />
        public ufbx_nurbs_surface** data;

        /// <include file='ufbx_nurbs_surface_list.xml' path='doc/member[@name="ufbx_nurbs_surface_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
