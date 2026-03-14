namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_const_transform_override_list
    {
        [NativeTypeName("const ufbx_transform_override *")]
        public ufbx_transform_override* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
