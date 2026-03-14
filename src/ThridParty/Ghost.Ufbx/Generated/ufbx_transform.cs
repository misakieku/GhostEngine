namespace Ghost.Ufbx
{
    public partial struct ufbx_transform
    {
        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 translation;

        [NativeTypeName("ufbx_quat")]
        public Misaki.HighPerformance.Mathematics.quaternion rotation;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 scale;
    }
}
