namespace Ghost.Ufbx
{
    /// <include file='ufbx_blend_keyframe.xml' path='doc/member[@name="ufbx_blend_keyframe"]/*' />
    public unsafe partial struct ufbx_blend_keyframe
    {
        /// <include file='ufbx_blend_keyframe.xml' path='doc/member[@name="ufbx_blend_keyframe.shape"]/*' />
        public ufbx_blend_shape* shape;

        /// <include file='ufbx_blend_keyframe.xml' path='doc/member[@name="ufbx_blend_keyframe.target_weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float target_weight;

        /// <include file='ufbx_blend_keyframe.xml' path='doc/member[@name="ufbx_blend_keyframe.effective_weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float effective_weight;
    }
}
