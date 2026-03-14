namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_skin_weight_list
    {
        public ufbx_skin_weight* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
