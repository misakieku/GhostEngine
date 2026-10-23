namespace Ghost.Ufbx
{
    /// <include file='ufbx_surface_point.xml' path='doc/member[@name="ufbx_surface_point"]/*' />
    public partial struct ufbx_surface_point
    {
        /// <include file='ufbx_surface_point.xml' path='doc/member[@name="ufbx_surface_point.valid"]/*' />
        [NativeTypeName("_Bool")]
        public bool valid;

        /// <include file='ufbx_surface_point.xml' path='doc/member[@name="ufbx_surface_point.position"]/*' />
        public ufbx_vec3 position;

        /// <include file='ufbx_surface_point.xml' path='doc/member[@name="ufbx_surface_point.derivative_u"]/*' />
        public ufbx_vec3 derivative_u;

        /// <include file='ufbx_surface_point.xml' path='doc/member[@name="ufbx_surface_point.derivative_v"]/*' />
        public ufbx_vec3 derivative_v;
    }
}
