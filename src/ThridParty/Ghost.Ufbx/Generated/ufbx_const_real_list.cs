namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_const_real_list
    {
        [NativeTypeName("const ufbx_real *")]
        public float* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
