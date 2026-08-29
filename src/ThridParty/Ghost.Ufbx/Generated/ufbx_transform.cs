namespace Ghost.Ufbx
{
    /// <include file='ufbx_transform.xml' path='doc/member[@name="ufbx_transform"]/*' />
    public partial struct ufbx_transform
    {
        /// <include file='ufbx_transform.xml' path='doc/member[@name="ufbx_transform.translation"]/*' />
        public ufbx_vec3 translation;

        /// <include file='ufbx_transform.xml' path='doc/member[@name="ufbx_transform.rotation"]/*' />
        public ufbx_quat rotation;

        /// <include file='ufbx_transform.xml' path='doc/member[@name="ufbx_transform.scale"]/*' />
        public ufbx_vec3 scale;
    }
}
