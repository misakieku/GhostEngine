namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_vec2_list
    {
        [NativeTypeName("ufbx_vec2 *")]
        public Misaki.HighPerformance.Mathematics.float2* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
