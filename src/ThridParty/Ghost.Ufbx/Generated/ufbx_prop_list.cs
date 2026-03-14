namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_prop_list
    {
        public ufbx_prop* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
