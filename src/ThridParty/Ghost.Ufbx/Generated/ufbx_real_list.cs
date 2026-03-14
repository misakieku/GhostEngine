namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_real_list
    {
        [NativeTypeName("ufbx_real *")]
        public float* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
