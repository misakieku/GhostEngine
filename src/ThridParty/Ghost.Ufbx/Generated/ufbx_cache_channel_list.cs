namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_cache_channel_list
    {
        public ufbx_cache_channel* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
