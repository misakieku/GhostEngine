namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_prop_override_list
    {
        public ufbx_prop_override* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
