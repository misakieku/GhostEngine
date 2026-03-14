namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_video_list
    {
        public ufbx_video** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
