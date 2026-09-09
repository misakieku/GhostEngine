namespace Ghost.Ufbx
{
    /// <include file='ufbx_lod_level.xml' path='doc/member[@name="ufbx_lod_level"]/*' />
    public partial struct ufbx_lod_level
    {
        /// <include file='ufbx_lod_level.xml' path='doc/member[@name="ufbx_lod_level.distance"]/*' />
        [NativeTypeName("ufbx_real")]
        public float distance;

        /// <include file='ufbx_lod_level.xml' path='doc/member[@name="ufbx_lod_level.display"]/*' />
        public ufbx_lod_display display;
    }
}
