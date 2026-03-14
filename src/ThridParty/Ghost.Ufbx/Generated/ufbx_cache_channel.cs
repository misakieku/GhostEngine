namespace Ghost.Ufbx
{
    public partial struct ufbx_cache_channel
    {
        public ufbx_string name;

        public ufbx_cache_interpretation interpretation;

        public ufbx_string interpretation_name;

        public ufbx_cache_frame_list frames;

        public ufbx_mirror_axis mirror_axis;

        [NativeTypeName("ufbx_real")]
        public float scale_factor;
    }
}
