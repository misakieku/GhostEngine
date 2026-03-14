namespace Ghost.Ufbx
{
    public partial struct ufbx_baked_vec3
    {
        public double time;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 value;

        public ufbx_baked_key_flags flags;
    }
}
