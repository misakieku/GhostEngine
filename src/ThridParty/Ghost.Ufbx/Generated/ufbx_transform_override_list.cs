namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_transform_override_list
    {
        public ufbx_transform_override* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
