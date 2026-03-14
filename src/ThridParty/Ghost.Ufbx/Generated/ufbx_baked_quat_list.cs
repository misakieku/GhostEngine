namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_baked_quat_list
    {
        public ufbx_baked_quat* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
