namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_subdivision_weight_list
    {
        public ufbx_subdivision_weight* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
