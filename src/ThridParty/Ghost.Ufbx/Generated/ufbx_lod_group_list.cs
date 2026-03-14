namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_lod_group_list
    {
        public ufbx_lod_group** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
