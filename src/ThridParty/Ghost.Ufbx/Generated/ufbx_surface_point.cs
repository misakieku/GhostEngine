namespace Ghost.Ufbx
{
    public partial struct ufbx_surface_point
    {
        [NativeTypeName("_Bool")]
        public bool valid;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 position;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 derivative_u;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 derivative_v;
    }
}
