namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_float_list
    {
        public float* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
