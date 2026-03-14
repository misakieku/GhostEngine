namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_cache_file_list
    {
        public ufbx_cache_file** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
