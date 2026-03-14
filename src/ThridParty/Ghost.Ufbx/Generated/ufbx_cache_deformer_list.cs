namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_cache_deformer_list
    {
        public ufbx_cache_deformer** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
