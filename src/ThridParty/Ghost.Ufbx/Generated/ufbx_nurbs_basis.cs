namespace Ghost.Ufbx
{
    /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis"]/*' />
    public partial struct ufbx_nurbs_basis
    {
        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.order"]/*' />
        [NativeTypeName("uint32_t")]
        public uint order;

        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.topology"]/*' />
        public ufbx_nurbs_topology topology;

        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.knot_vector"]/*' />
        public ufbx_real_list knot_vector;

        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.t_min"]/*' />
        [NativeTypeName("ufbx_real")]
        public float t_min;

        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.t_max"]/*' />
        [NativeTypeName("ufbx_real")]
        public float t_max;

        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.spans"]/*' />
        public ufbx_real_list spans;

        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.is_2d"]/*' />
        [NativeTypeName("_Bool")]
        public bool is_2d;

        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.num_wrap_control_points"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_wrap_control_points;

        /// <include file='ufbx_nurbs_basis.xml' path='doc/member[@name="ufbx_nurbs_basis.valid"]/*' />
        [NativeTypeName("_Bool")]
        public bool valid;
    }
}
