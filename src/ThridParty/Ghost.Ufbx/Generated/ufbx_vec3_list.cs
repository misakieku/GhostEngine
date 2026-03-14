namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_vec3_list
    {
        [NativeTypeName("ufbx_vec3 *")]
        public Misaki.HighPerformance.Mathematics.float3* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
