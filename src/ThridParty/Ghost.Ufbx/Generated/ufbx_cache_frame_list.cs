namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_cache_frame_list
    {
        public ufbx_cache_frame* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
