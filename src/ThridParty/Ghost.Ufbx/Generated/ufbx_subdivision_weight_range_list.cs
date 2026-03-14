namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_subdivision_weight_range_list
    {
        public ufbx_subdivision_weight_range* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
