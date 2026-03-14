namespace Ghost.Ufbx
{
    public partial struct ufbx_baked_quat
    {
        public double time;

        [NativeTypeName("ufbx_quat")]
        public Misaki.HighPerformance.Mathematics.quaternion value;

        public ufbx_baked_key_flags flags;
    }
}
