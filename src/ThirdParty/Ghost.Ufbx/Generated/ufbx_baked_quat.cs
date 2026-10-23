namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_quat.xml' path='doc/member[@name="ufbx_baked_quat"]/*' />
    public partial struct ufbx_baked_quat
    {
        /// <include file='ufbx_baked_quat.xml' path='doc/member[@name="ufbx_baked_quat.time"]/*' />
        public double time;

        /// <include file='ufbx_baked_quat.xml' path='doc/member[@name="ufbx_baked_quat.value"]/*' />
        public ufbx_quat value;

        /// <include file='ufbx_baked_quat.xml' path='doc/member[@name="ufbx_baked_quat.flags"]/*' />
        public ufbx_baked_key_flags flags;
    }
}
