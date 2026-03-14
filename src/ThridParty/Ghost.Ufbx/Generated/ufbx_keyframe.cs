namespace Ghost.Ufbx
{
    public partial struct ufbx_keyframe
    {
        public double time;

        [NativeTypeName("ufbx_real")]
        public float value;

        public ufbx_interpolation interpolation;

        public ufbx_tangent left;

        public ufbx_tangent right;
    }
}
