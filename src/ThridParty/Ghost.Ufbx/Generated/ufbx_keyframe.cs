namespace Ghost.Ufbx
{
    /// <include file='ufbx_keyframe.xml' path='doc/member[@name="ufbx_keyframe"]/*' />
    public partial struct ufbx_keyframe
    {
        /// <include file='ufbx_keyframe.xml' path='doc/member[@name="ufbx_keyframe.time"]/*' />
        public double time;

        /// <include file='ufbx_keyframe.xml' path='doc/member[@name="ufbx_keyframe.value"]/*' />
        [NativeTypeName("ufbx_real")]
        public float value;

        /// <include file='ufbx_keyframe.xml' path='doc/member[@name="ufbx_keyframe.interpolation"]/*' />
        public ufbx_interpolation interpolation;

        /// <include file='ufbx_keyframe.xml' path='doc/member[@name="ufbx_keyframe.left"]/*' />
        public ufbx_tangent left;

        /// <include file='ufbx_keyframe.xml' path='doc/member[@name="ufbx_keyframe.right"]/*' />
        public ufbx_tangent right;
    }
}
