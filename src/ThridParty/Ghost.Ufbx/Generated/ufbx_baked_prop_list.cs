namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_baked_prop_list
    {
        public ufbx_baked_prop* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
