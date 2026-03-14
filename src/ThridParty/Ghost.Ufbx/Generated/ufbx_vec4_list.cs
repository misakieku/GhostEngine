namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_vec4_list
    {
        [NativeTypeName("ufbx_vec4 *")]
        public Misaki.HighPerformance.Mathematics.float4* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
