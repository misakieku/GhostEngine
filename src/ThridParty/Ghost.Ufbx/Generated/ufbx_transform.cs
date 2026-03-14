namespace Ghost.Ufbx
{
    public partial struct ufbx_transform
    {
        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 translation;

        public ufbx_quat rotation;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 scale;
    }
}
