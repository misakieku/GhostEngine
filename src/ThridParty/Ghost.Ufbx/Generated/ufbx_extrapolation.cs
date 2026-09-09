namespace Ghost.Ufbx
{
    /// <include file='ufbx_extrapolation.xml' path='doc/member[@name="ufbx_extrapolation"]/*' />
    public partial struct ufbx_extrapolation
    {
        /// <include file='ufbx_extrapolation.xml' path='doc/member[@name="ufbx_extrapolation.mode"]/*' />
        public ufbx_extrapolation_mode mode;

        /// <include file='ufbx_extrapolation.xml' path='doc/member[@name="ufbx_extrapolation.repeat_count"]/*' />
        [NativeTypeName("int32_t")]
        public int repeat_count;
    }
}
