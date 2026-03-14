namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_double_list
    {
        public double* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
