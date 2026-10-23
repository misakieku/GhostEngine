namespace Ghost.Ufbx
{
    /// <include file='ufbx_curve_point.xml' path='doc/member[@name="ufbx_curve_point"]/*' />
    public partial struct ufbx_curve_point
    {
        /// <include file='ufbx_curve_point.xml' path='doc/member[@name="ufbx_curve_point.valid"]/*' />
        [NativeTypeName("_Bool")]
        public bool valid;

        /// <include file='ufbx_curve_point.xml' path='doc/member[@name="ufbx_curve_point.position"]/*' />
        public ufbx_vec3 position;

        /// <include file='ufbx_curve_point.xml' path='doc/member[@name="ufbx_curve_point.derivative"]/*' />
        public ufbx_vec3 derivative;
    }
}
