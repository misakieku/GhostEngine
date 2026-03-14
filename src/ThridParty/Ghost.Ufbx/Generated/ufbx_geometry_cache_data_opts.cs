namespace Ghost.Ufbx
{
    public partial struct ufbx_geometry_cache_data_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_open_file_cb open_file_cb;

        [NativeTypeName("_Bool")]
        public bool additive;

        [NativeTypeName("_Bool")]
        public bool use_weight;

        [NativeTypeName("ufbx_real")]
        public float weight;

        [NativeTypeName("_Bool")]
        public bool ignore_transform;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
