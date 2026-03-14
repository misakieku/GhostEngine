namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_lod_level_list
    {
        public ufbx_lod_level* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
