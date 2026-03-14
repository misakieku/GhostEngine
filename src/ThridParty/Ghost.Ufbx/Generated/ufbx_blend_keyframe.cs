namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_blend_keyframe
    {
        public ufbx_blend_shape* shape;

        [NativeTypeName("ufbx_real")]
        public float target_weight;

        [NativeTypeName("ufbx_real")]
        public float effective_weight;
    }
}
